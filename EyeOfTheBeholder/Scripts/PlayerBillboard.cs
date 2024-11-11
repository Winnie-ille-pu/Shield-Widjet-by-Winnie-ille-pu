using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Serialization;

public class PlayerBillboard : MonoBehaviour
{
    static readonly Type customBlendModeType = typeof(MaterialReader.CustomBlendMode);

    Transform torch = null;
    Vector3 torchPosLocalDefault;

    public static PlayerBillboard Instance;

    public bool IsReady
    {
        get
        {
            if (transform.parent != null)
                return true;
            else
                return false;
        }
    }

    const int numberOrientations = 8;
    const float anglePerOrientation = 360f / numberOrientations;

    float sizeMod
    {
        get
        {
            if (riding || transformed)
                return 0.029f * scale;
            else
                return 0.019f * scale;
        }
    }
    float scale = 1;
    float scaleOffsetMod = 1;
    float scaleOffset
    {
        get
        {
            return scale * scaleOffsetMod;
        }
    }

    Camera mainCamera = null;
    MeshFilter meshFilter = null;
    MeshRenderer meshRenderer = null;

    public Vector3 cameraPosition;
    public float currentAngle;
    public Vector3 lastMoveDirection;
    public int lastOrientation;
    public Color lastColor;

    float orientationTime = 0.1f;
    float orientationTimer;

    int frameCurrent;
    float frameTime
    {
        get
        {
            if (riding)
                return 0.0625f * (2-walkAnimSpeedMod);
            else
                return 0.25f * (2-walkAnimSpeedMod);
        }
    }
    float frameTimer;

    public PlayerBillboardState[] stateCurrent;
    public PlayerBillboardState[] stateLast;

    PlayerBillboardState[] StatesIdle;
    PlayerBillboardState[] StatesReadyMelee;
    PlayerBillboardState[] StatesReadyRanged;
    PlayerBillboardState[] StatesReadySpell;
    PlayerBillboardState[] StatesMove;
    PlayerBillboardState[] StatesMoveMelee;
    PlayerBillboardState[] StatesMoveRanged;
    PlayerBillboardState[] StatesMoveSpell;
    PlayerBillboardState[] StatesAttackMelee;
    PlayerBillboardState[] StatesAttackRanged;
    PlayerBillboardState[] StatesAttackSpell;
    PlayerBillboardState[] StatesDeath;

    PlayerBillboardState[] StatesIdleHorse;
    PlayerBillboardState[] StatesMoveHorse;

    PlayerBillboardState[] StatesIdleLycan;
    PlayerBillboardState[] StatesReadyLycan;
    PlayerBillboardState[] StatesMoveLycan;
    PlayerBillboardState[] StatesAttackLycan;
    PlayerBillboardState[] StatesDeathLycan;

    public bool lengthsChanged = false;
    public int StatesIdleLength = 1;
    public Vector2 StatesIdleOffset = -Vector2.one;
    public int StatesReadyMeleeLength = 1;
    public int StatesReadyRangedLength = 1;
    public int StatesReadySpellLength = 1;
    public int StatesMoveLength = 4;
    public Vector2 StatesMoveOffset = -Vector2.one;
    public int StatesMoveMeleeLength = 2;
    public int StatesMoveRangedLength = 2;
    public int StatesMoveSpellLength = 2;
    public int StatesAttackMeleeLength = 6;
    public Vector2 StatesAttackMeleeOffset = -Vector2.one;
    public int StatesAttackRangedLength = 4;
    public Vector2 StatesAttackRangedOffset = -Vector2.one;
    public int StatesAttackSpellLength = 4;
    public Vector2 StatesAttackSpellOffset = -Vector2.one;
    public int StatesDeathLength = 2;
    public Vector2 StatesDeathOffset = -Vector2.one;
    public int StatesIdleHorseLength = 1;
    public Vector2 StatesIdleHorseOffset = -Vector2.one;
    public int StatesMoveHorseLength = 8;
    public Vector2 StatesMoveHorseOffset = -Vector2.one;
    public int StatesIdleLycanLength = 1;
    public Vector2 StatesIdleLycanOffset = -Vector2.one;
    public int StatesReadyLycanLength = 1;
    public int StatesMoveLycanLength = 4;
    public Vector2 StatesMoveLycanOffset = -Vector2.one;
    public int StatesAttackLycanLength = 3;
    public Vector2 StatesAttackLycanOffset = -Vector2.one;
    public int StatesDeathLycanLength = 2;
    public Vector2 StatesDeathLycanOffset = -Vector2.one;

    IEnumerator isAnimating;
    bool died;
    bool riding;
    bool sheathed;
    bool usingBow;
    bool spellcasting;
    bool stopped;
    bool transformed;
    bool floating;
    bool animating;

    int indexFoot = -1;
    int indexHorse = -1;
    //int indexLycan = 0;

    //when will the billboard orientation turn to the view
    public int readyStance = 0; //0 = never, 1 = on idle, 2 = on idle and moving
    public int turnToView = 0; //0 = never, 1 = only when animating, 2 = also when weapon drawn, 3 = always

    public Vector2 offsetDefault = -Vector2.one;
    public Vector2 offsetAttack = -Vector2.one;

    Shader shaderNormal;
    Shader shaderGhost;

    public int attackStrings;
    public float mirrorTime = 3;
    public int pingpongOffset = 0;
    int pingpongCount = 0;

    float mirrorTimer;
    int mirrorCount = 0;

    public bool FP;
    public int torchOffset;
    public float walkAnimSpeedMod;

    public bool footsteps;
    SoundClips footstepSound1;
    SoundClips footstepSound2;
    AudioClip footstep1;
    AudioClip footstep2;
    bool footstepAlt;

    bool hasPlayedFootstep;

    public class PlayerBillboardState
    {
        public string name;
        public int length;
        public Texture2D[] frames;
        public Vector2 offset;
        public bool flipped;

        public PlayerBillboardState (string newName, int newLength, int archive, int record, int frame , Vector2 newOffset, bool newFlipped)
        {
            name = newName;
            length = newLength;
            offset = newOffset;
            flipped = newFlipped;

            InitializeTextures(archive,record,frame);

            Debug.Log("PlayerBillboardState " + name + " was initialized with " + frames.Length + " frames");
        }

        public Rect GetRect(int frame, bool flip)
        {
            if (flip)
                return new Rect(-offset.x, offset.y, frames[frame].width, frames[frame].height);
            else
                return new Rect(offset.x, offset.y, frames[frame].width, frames[frame].height);
        }

        void InitializeTextures(int archive, int record, int frame)
        {
            frames = new Texture2D[length];
            int current = frame;
            bool reverse = false;
            for (int i = 0; i < length; i++)
            {
                Texture2D texture;
                if (DaggerfallWorkshop.Utility.AssetInjection.TextureReplacement.TryImportTexture(archive, record, current, out texture))
                {
                    frames[i] = texture;
                    //current++;

                    if (reverse)
                        current--;
                    else
                        current++;
                } else
                {
                    /*length = i;
                    break;*/

                    Debug.Log(name + " - No texture in frame! Reversing!");
                    reverse = !reverse;
                    if (reverse)
                        current--;
                    else
                        current++;
                    i -= 2;
                }
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        SaveLoadManager.OnLoad += OnLoad;
    }

    public static void OnLoad(SaveData_v1 saveData)
    {
        Instance.InitializeStates();
    }

    void InitializeStates()
    {
        died = false;

        int indexLycan = 0;
        if (GameManager.Instance.PlayerEffectManager.LycanthropyType() == LycanthropyTypes.Wereboar)
            indexLycan = 1;

        int archiveFoot = 3800 + indexFoot;
        int archiveHorse = 3900 + indexHorse;
        int archiveLycan = 3816 + indexLycan;

        StatesIdle = new PlayerBillboardState[numberOrientations];
        StatesIdle[0] = new PlayerBillboardState("IdleForward", StatesIdleLength, archiveFoot, 15, 0, StatesIdleOffset*scaleOffset, false);
        StatesIdle[1] = new PlayerBillboardState("IdleForwardLeft", StatesIdleLength, archiveFoot, 16, 0, StatesIdleOffset*scaleOffset, true);
        StatesIdle[2] = new PlayerBillboardState("IdleLeft", StatesIdleLength, archiveFoot, 17, 0, StatesIdleOffset*scaleOffset, true);
        StatesIdle[3] = new PlayerBillboardState("IdleBackwardLeft", StatesIdleLength, archiveFoot, 18, 0, StatesIdleOffset*scaleOffset, true);
        StatesIdle[4] = new PlayerBillboardState("IdleBackward", StatesIdleLength, archiveFoot, 19, 0, StatesIdleOffset*scaleOffset, false);
        StatesIdle[5] = new PlayerBillboardState("IdleBackwardRight", StatesIdleLength, archiveFoot, 18, 0, StatesIdleOffset*scaleOffset, false);
        StatesIdle[6] = new PlayerBillboardState("IdleRight", StatesIdleLength, archiveFoot, 17, 0, StatesIdleOffset*scaleOffset, false);
        StatesIdle[7] = new PlayerBillboardState("IdleForwardRight", StatesIdleLength, archiveFoot, 16, 0, StatesIdleOffset*scaleOffset, false);

        StatesReadyMelee = new PlayerBillboardState[numberOrientations];
        StatesReadyMelee[0] = new PlayerBillboardState("ReadyMeleeForward", StatesReadyMeleeLength, archiveFoot, 5, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesReadyMelee[1] = new PlayerBillboardState("ReadyMeleeForwardLeft", StatesReadyMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesReadyMelee[2] = new PlayerBillboardState("ReadyMeleeLeft", StatesReadyMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesReadyMelee[3] = new PlayerBillboardState("ReadyMeleeBackwardLeft", StatesReadyMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesReadyMelee[4] = new PlayerBillboardState("ReadyMeleeBackward", StatesReadyMeleeLength, archiveFoot, 9, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesReadyMelee[5] = new PlayerBillboardState("ReadyMeleeBackwardRight", StatesReadyMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesReadyMelee[6] = new PlayerBillboardState("ReadyMeleeRight", StatesReadyMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesReadyMelee[7] = new PlayerBillboardState("ReadyMeleeForwardRight", StatesReadyMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, false);

        StatesReadyRanged = new PlayerBillboardState[numberOrientations];
        StatesReadyRanged[0] = new PlayerBillboardState("ReadyRangedForward", StatesReadyRangedLength, archiveFoot, 25, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesReadyRanged[1] = new PlayerBillboardState("ReadyRangedForwardLeft", StatesReadyRangedLength, archiveFoot, 26, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesReadyRanged[2] = new PlayerBillboardState("ReadyRangedLeft", StatesReadyRangedLength, archiveFoot, 27, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesReadyRanged[3] = new PlayerBillboardState("ReadyRangedBackwardLeft", StatesReadyRangedLength, archiveFoot, 28, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesReadyRanged[4] = new PlayerBillboardState("ReadyRangedBackward", StatesReadyRangedLength, archiveFoot, 29, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesReadyRanged[5] = new PlayerBillboardState("ReadyRangedBackwardRight", StatesReadyRangedLength, archiveFoot, 28, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesReadyRanged[6] = new PlayerBillboardState("ReadyRangedRight", StatesReadyRangedLength, archiveFoot, 27, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesReadyRanged[7] = new PlayerBillboardState("ReadyRangedForwardRight", StatesReadyRangedLength, archiveFoot, 26, 3, StatesAttackRangedOffset*scaleOffset, false);

        StatesReadySpell = new PlayerBillboardState[numberOrientations];
        StatesReadySpell[0] = new PlayerBillboardState("ReadySpellForward", StatesReadySpellLength, archiveFoot, 20, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesReadySpell[1] = new PlayerBillboardState("ReadySpellForwardLeft", StatesReadySpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesReadySpell[2] = new PlayerBillboardState("ReadySpellLeft", StatesReadySpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesReadySpell[3] = new PlayerBillboardState("ReadySpellBackwardLeft", StatesReadySpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesReadySpell[4] = new PlayerBillboardState("ReadySpellBackward", StatesReadySpellLength, archiveFoot, 24, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesReadySpell[5] = new PlayerBillboardState("ReadySpellBackwardRight", StatesReadySpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesReadySpell[6] = new PlayerBillboardState("ReadySpellRight", StatesReadySpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesReadySpell[7] = new PlayerBillboardState("ReadySpellForwardRight", StatesReadySpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, false);

        StatesMove = new PlayerBillboardState[numberOrientations];
        StatesMove[0] = new PlayerBillboardState("MoveForward", StatesMoveLength, archiveFoot, 0, 0, StatesMoveOffset*scaleOffset, false);
        StatesMove[1] = new PlayerBillboardState("MoveForwardLeft", StatesMoveLength, archiveFoot, 1, 0, StatesMoveOffset*scaleOffset, true);
        StatesMove[2] = new PlayerBillboardState("MoveLeft", StatesMoveLength, archiveFoot, 2, 0, StatesMoveOffset*scaleOffset, true);
        StatesMove[3] = new PlayerBillboardState("MoveBackwardLeft", StatesMoveLength, archiveFoot, 3, 0, StatesMoveOffset*scaleOffset, true);
        StatesMove[4] = new PlayerBillboardState("MoveBackward", StatesMoveLength, archiveFoot, 4, 0, StatesMoveOffset*scaleOffset, false);
        StatesMove[5] = new PlayerBillboardState("MoveBackwardRight", StatesMoveLength, archiveFoot, 3, 0, StatesMoveOffset*scaleOffset, false);
        StatesMove[6] = new PlayerBillboardState("MoveRight", StatesMoveLength, archiveFoot, 2, 0, StatesMoveOffset*scaleOffset, false);
        StatesMove[7] = new PlayerBillboardState("MoveForwardRight", StatesMoveLength, archiveFoot, 1, 0, StatesMoveOffset*scaleOffset, false);

        StatesMoveMelee = new PlayerBillboardState[numberOrientations];
        StatesMoveMelee[0] = new PlayerBillboardState("MoveMeleeForward", StatesMoveMeleeLength, archiveFoot, 5, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesMoveMelee[1] = new PlayerBillboardState("MoveMeleeForwardLeft", StatesMoveMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesMoveMelee[2] = new PlayerBillboardState("MoveMeleeLeft", StatesMoveMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesMoveMelee[3] = new PlayerBillboardState("MoveMeleeBackwardLeft", StatesMoveMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesMoveMelee[4] = new PlayerBillboardState("MoveMeleeBackward", StatesMoveMeleeLength, archiveFoot, 9, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesMoveMelee[5] = new PlayerBillboardState("MoveMeleeBackwardRight", StatesMoveMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesMoveMelee[6] = new PlayerBillboardState("MoveMeleeRight", StatesMoveMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesMoveMelee[7] = new PlayerBillboardState("MoveMeleeForwardRight", StatesMoveMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, false);

        StatesMoveRanged = new PlayerBillboardState[numberOrientations];
        StatesMoveRanged[0] = new PlayerBillboardState("MoveRangedForward", StatesMoveRangedLength, archiveFoot, 25, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesMoveRanged[1] = new PlayerBillboardState("MoveRangedForwardLeft", StatesMoveRangedLength, archiveFoot, 26, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesMoveRanged[2] = new PlayerBillboardState("MoveRangedLeft", StatesMoveRangedLength, archiveFoot, 27, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesMoveRanged[3] = new PlayerBillboardState("MoveRangedBackwardLeft", StatesMoveRangedLength, archiveFoot, 28, 3, StatesAttackRangedOffset*scaleOffset, true);
        StatesMoveRanged[4] = new PlayerBillboardState("MoveRangedBackward", StatesMoveRangedLength, archiveFoot, 29, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesMoveRanged[5] = new PlayerBillboardState("MoveRangedBackwardRight", StatesMoveRangedLength, archiveFoot, 28, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesMoveRanged[6] = new PlayerBillboardState("MoveRangedRight", StatesMoveRangedLength, archiveFoot, 27, 3, StatesAttackRangedOffset*scaleOffset, false);
        StatesMoveRanged[7] = new PlayerBillboardState("MoveRangedForwardRight", StatesMoveRangedLength, archiveFoot, 26, 3, StatesAttackRangedOffset*scaleOffset, false);

        StatesMoveSpell = new PlayerBillboardState[numberOrientations];
        StatesMoveSpell[0] = new PlayerBillboardState("MoveSpellForward", StatesMoveSpellLength, archiveFoot, 20, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesMoveSpell[1] = new PlayerBillboardState("MoveSpellForwardLeft", StatesMoveSpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesMoveSpell[2] = new PlayerBillboardState("MoveSpellLeft", StatesMoveSpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesMoveSpell[3] = new PlayerBillboardState("MoveSpellBackwardLeft", StatesMoveSpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesMoveSpell[4] = new PlayerBillboardState("MoveSpellBackward", StatesMoveSpellLength, archiveFoot, 24, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesMoveSpell[5] = new PlayerBillboardState("MoveSpellBackwardRight", StatesMoveSpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesMoveSpell[6] = new PlayerBillboardState("MoveSpellRight", StatesMoveSpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesMoveSpell[7] = new PlayerBillboardState("MoveSpellForwardRight", StatesMoveSpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, false);

        StatesAttackMelee = new PlayerBillboardState[numberOrientations];
        StatesAttackMelee[0] = new PlayerBillboardState("AttackMeleeForward", StatesAttackMeleeLength, archiveFoot, 5, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesAttackMelee[1] = new PlayerBillboardState("AttackMeleeForwardLeft", StatesAttackMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesAttackMelee[2] = new PlayerBillboardState("AttackMeleeLeft", StatesAttackMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesAttackMelee[3] = new PlayerBillboardState("AttackMeleeBackwardLeft", StatesAttackMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, true);
        StatesAttackMelee[4] = new PlayerBillboardState("AttackMeleeBackward", StatesAttackMeleeLength, archiveFoot, 9, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesAttackMelee[5] = new PlayerBillboardState("AttackMeleeBackwardRight", StatesAttackMeleeLength, archiveFoot, 8, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesAttackMelee[6] = new PlayerBillboardState("AttackMeleeRight", StatesAttackMeleeLength, archiveFoot, 7, 0, StatesAttackMeleeOffset*scaleOffset, false);
        StatesAttackMelee[7] = new PlayerBillboardState("AttackMeleeForwardRight", StatesAttackMeleeLength, archiveFoot, 6, 0, StatesAttackMeleeOffset*scaleOffset, false);

        StatesAttackRanged = new PlayerBillboardState[numberOrientations];
        StatesAttackRanged[0] = new PlayerBillboardState("AttackRangedForward", StatesAttackRangedLength, archiveFoot, 25, 0, StatesAttackRangedOffset*scaleOffset, false);
        StatesAttackRanged[1] = new PlayerBillboardState("AttackRangedForwardLeft", StatesAttackRangedLength, archiveFoot, 26, 0, StatesAttackRangedOffset*scaleOffset, true);
        StatesAttackRanged[2] = new PlayerBillboardState("AttackRangedLeft", StatesAttackRangedLength, archiveFoot, 27, 0, StatesAttackRangedOffset*scaleOffset, true);
        StatesAttackRanged[3] = new PlayerBillboardState("AttackRangedBackwardLeft", StatesAttackRangedLength, archiveFoot, 28, 0, StatesAttackRangedOffset*scaleOffset, true);
        StatesAttackRanged[4] = new PlayerBillboardState("AttackRangedBackward", StatesAttackRangedLength, archiveFoot, 29, 0, StatesAttackRangedOffset*scaleOffset, false);
        StatesAttackRanged[5] = new PlayerBillboardState("AttackRangedBackwardRight", StatesAttackRangedLength, archiveFoot, 28, 0, StatesAttackRangedOffset*scaleOffset, false);
        StatesAttackRanged[6] = new PlayerBillboardState("AttackRangedRight", StatesAttackRangedLength, archiveFoot, 27, 0, StatesAttackRangedOffset*scaleOffset, false);
        StatesAttackRanged[7] = new PlayerBillboardState("AttackRangedForwardRight", StatesAttackRangedLength, archiveFoot, 26, 0, StatesAttackRangedOffset*scaleOffset, false);

        StatesAttackSpell = new PlayerBillboardState[numberOrientations];
        StatesAttackSpell[0] = new PlayerBillboardState("AttackSpellForward", StatesAttackSpellLength, archiveFoot, 20, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesAttackSpell[1] = new PlayerBillboardState("AttackSpellForwardLeft", StatesAttackSpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesAttackSpell[2] = new PlayerBillboardState("AttackSpellLeft", StatesAttackSpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesAttackSpell[3] = new PlayerBillboardState("AttackSpellBackwardLeft", StatesAttackSpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, true);
        StatesAttackSpell[4] = new PlayerBillboardState("AttackSpellBackward", StatesAttackSpellLength, archiveFoot, 24, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesAttackSpell[5] = new PlayerBillboardState("AttackSpellBackwardRight", StatesAttackSpellLength, archiveFoot, 23, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesAttackSpell[6] = new PlayerBillboardState("AttackSpellRight", StatesAttackSpellLength, archiveFoot, 22, 0, StatesAttackSpellOffset*scaleOffset, false);
        StatesAttackSpell[7] = new PlayerBillboardState("AttackSpellForwardRight", StatesAttackSpellLength, archiveFoot, 21, 0, StatesAttackSpellOffset*scaleOffset, false);

        StatesDeath = new PlayerBillboardState[numberOrientations];
        StatesDeath[0] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 10, 0, StatesDeathOffset*scaleOffset, false);
        StatesDeath[1] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 11, 0, StatesDeathOffset*scaleOffset, true);
        StatesDeath[2] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 12, 0, StatesDeathOffset*scaleOffset, true);
        StatesDeath[3] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 13, 0, StatesDeathOffset*scaleOffset, true);
        StatesDeath[4] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 14, 0, StatesDeathOffset*scaleOffset, false);
        StatesDeath[5] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 13, 0, StatesDeathOffset*scaleOffset, false);
        StatesDeath[6] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 12, 0, StatesDeathOffset*scaleOffset, false);
        StatesDeath[7] = new PlayerBillboardState("Death", StatesDeathLength, archiveFoot, 11, 0, StatesDeathOffset*scaleOffset, false);

        StatesIdleHorse = new PlayerBillboardState[numberOrientations];
        StatesIdleHorse[0] = new PlayerBillboardState("IdleForward", StatesIdleHorseLength, archiveHorse, 0, 0, StatesIdleHorseOffset*scaleOffset, false);
        StatesIdleHorse[1] = new PlayerBillboardState("IdleForwardLeft", StatesIdleHorseLength, archiveHorse, 1, 0, StatesIdleHorseOffset*scaleOffset, true);
        StatesIdleHorse[2] = new PlayerBillboardState("IdleLeft", StatesIdleHorseLength, archiveHorse, 2, 0, StatesIdleHorseOffset*scaleOffset, true);
        StatesIdleHorse[3] = new PlayerBillboardState("IdleBackwardLeft", StatesIdleHorseLength, archiveHorse, 3, 0, StatesIdleHorseOffset*scaleOffset, true);
        StatesIdleHorse[4] = new PlayerBillboardState("IdleBackward", StatesIdleHorseLength, archiveHorse, 4, 0, StatesIdleHorseOffset*scaleOffset, false);
        StatesIdleHorse[5] = new PlayerBillboardState("IdleBackwardRight", StatesIdleHorseLength, archiveHorse, 3, 0, StatesIdleHorseOffset*scaleOffset, false);
        StatesIdleHorse[6] = new PlayerBillboardState("IdleRight", StatesIdleHorseLength, archiveHorse, 2, 0, StatesIdleHorseOffset*scaleOffset, false);
        StatesIdleHorse[7] = new PlayerBillboardState("IdleForwardRight", StatesIdleHorseLength, archiveHorse, 1, 0, StatesIdleHorseOffset*scaleOffset, false);

        StatesMoveHorse = new PlayerBillboardState[numberOrientations];
        StatesMoveHorse[0] = new PlayerBillboardState("MoveForward", StatesMoveHorseLength, archiveHorse, 0, 0, StatesMoveHorseOffset*scaleOffset, false);
        StatesMoveHorse[1] = new PlayerBillboardState("MoveForwardLeft", StatesMoveHorseLength, archiveHorse, 1, 0, StatesMoveHorseOffset*scaleOffset, true);
        StatesMoveHorse[2] = new PlayerBillboardState("MoveLeft", StatesMoveHorseLength, archiveHorse, 2, 0, StatesMoveHorseOffset*scaleOffset, true);
        StatesMoveHorse[3] = new PlayerBillboardState("MoveBackwardLeft", StatesMoveHorseLength, archiveHorse, 3, 0, StatesMoveHorseOffset*scaleOffset, true);
        StatesMoveHorse[4] = new PlayerBillboardState("MoveBackward", StatesMoveHorseLength, archiveHorse, 4, 0, StatesMoveHorseOffset*scaleOffset, false);
        StatesMoveHorse[5] = new PlayerBillboardState("MoveBackwardRight", StatesMoveHorseLength, archiveHorse, 3, 0, StatesMoveHorseOffset*scaleOffset, false);
        StatesMoveHorse[6] = new PlayerBillboardState("MoveRight", StatesMoveHorseLength, archiveHorse, 2, 0, StatesMoveHorseOffset*scaleOffset, false);
        StatesMoveHorse[7] = new PlayerBillboardState("MoveForwardRight", StatesMoveHorseLength, archiveHorse, 1, 0, StatesMoveHorseOffset*scaleOffset, false);

        StatesIdleLycan = new PlayerBillboardState[numberOrientations];
        StatesIdleLycan[0] = new PlayerBillboardState("IdleForward", StatesIdleLycanLength, archiveLycan, 0, 0, StatesIdleLycanOffset*scaleOffset, false);
        StatesIdleLycan[1] = new PlayerBillboardState("IdleForwardLeft", StatesIdleLycanLength, archiveLycan, 1, 0, StatesIdleLycanOffset*scaleOffset, true);
        StatesIdleLycan[2] = new PlayerBillboardState("IdleLeft", StatesIdleLycanLength, archiveLycan, 2, 0, StatesIdleLycanOffset*scaleOffset, true);
        StatesIdleLycan[3] = new PlayerBillboardState("IdleBackwardLeft", StatesIdleLycanLength, archiveLycan, 3, 0, StatesIdleLycanOffset*scaleOffset, true);
        StatesIdleLycan[4] = new PlayerBillboardState("IdleBackward", StatesIdleLycanLength, archiveLycan, 4, 0, StatesIdleLycanOffset*scaleOffset, false);
        StatesIdleLycan[5] = new PlayerBillboardState("IdleBackwardRight", StatesIdleLycanLength, archiveLycan, 3, 0, StatesIdleLycanOffset*scaleOffset, false);
        StatesIdleLycan[6] = new PlayerBillboardState("IdleRight", StatesIdleLycanLength, archiveLycan, 2, 0, StatesIdleLycanOffset*scaleOffset, false);
        StatesIdleLycan[7] = new PlayerBillboardState("IdleForwardRight", StatesIdleLycanLength, archiveLycan, 1, 0, StatesIdleLycanOffset*scaleOffset, false);

        StatesReadyLycan = new PlayerBillboardState[numberOrientations];
        StatesReadyLycan[0] = new PlayerBillboardState("ReadyLycanForward", StatesReadyLycanLength, archiveLycan, 5, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesReadyLycan[1] = new PlayerBillboardState("ReadyLycanForwardLeft", StatesReadyLycanLength, archiveLycan, 6, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesReadyLycan[2] = new PlayerBillboardState("ReadyLycanLeft", StatesReadyLycanLength, archiveLycan, 7, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesReadyLycan[3] = new PlayerBillboardState("ReadyLycanBackwardLeft", StatesReadyLycanLength, archiveLycan, 8, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesReadyLycan[4] = new PlayerBillboardState("ReadyLycanBackward", StatesReadyLycanLength, archiveLycan, 9, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesReadyLycan[5] = new PlayerBillboardState("ReadyLycanBackwardRight", StatesReadyLycanLength, archiveLycan, 8, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesReadyLycan[6] = new PlayerBillboardState("ReadyLycanRight", StatesReadyLycanLength, archiveLycan, 7, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesReadyLycan[7] = new PlayerBillboardState("ReadyLycanForwardRight", StatesReadyLycanLength, archiveLycan, 6, 0, StatesAttackLycanOffset*scaleOffset, false);

        StatesMoveLycan = new PlayerBillboardState[numberOrientations];
        StatesMoveLycan[0] = new PlayerBillboardState("MoveForward", StatesMoveLycanLength, archiveLycan, 0, 0, StatesMoveLycanOffset*scaleOffset, false);
        StatesMoveLycan[1] = new PlayerBillboardState("MoveForwardLeft", StatesMoveLycanLength, archiveLycan, 1, 0, StatesMoveLycanOffset*scaleOffset, true);
        StatesMoveLycan[2] = new PlayerBillboardState("MoveLeft", StatesMoveLycanLength, archiveLycan, 2, 0, StatesMoveLycanOffset*scaleOffset, true);
        StatesMoveLycan[3] = new PlayerBillboardState("MoveBackwardLeft", StatesMoveLycanLength, archiveLycan, 3, 0, StatesMoveLycanOffset*scaleOffset, true);
        StatesMoveLycan[4] = new PlayerBillboardState("MoveBackward", StatesMoveLycanLength, archiveLycan, 4, 0, StatesMoveLycanOffset*scaleOffset, false);
        StatesMoveLycan[5] = new PlayerBillboardState("MoveBackwardRight", StatesMoveLycanLength, archiveLycan, 3, 0, StatesMoveLycanOffset*scaleOffset, false);
        StatesMoveLycan[6] = new PlayerBillboardState("MoveRight", StatesMoveLycanLength, archiveLycan, 2, 0, StatesMoveLycanOffset*scaleOffset, false);
        StatesMoveLycan[7] = new PlayerBillboardState("MoveForwardRight", StatesMoveLycanLength, archiveLycan, 1, 0, StatesMoveLycanOffset*scaleOffset, false);

        StatesAttackLycan = new PlayerBillboardState[numberOrientations];
        StatesAttackLycan[0] = new PlayerBillboardState("AttackLycanForward", StatesAttackLycanLength, archiveLycan, 5, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesAttackLycan[1] = new PlayerBillboardState("AttackLycanForwardLeft", StatesAttackLycanLength, archiveLycan, 6, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesAttackLycan[2] = new PlayerBillboardState("AttackLycanLeft", StatesAttackLycanLength, archiveLycan, 7, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesAttackLycan[3] = new PlayerBillboardState("AttackLycanBackwardLeft", StatesAttackLycanLength, archiveLycan, 8, 0, StatesAttackLycanOffset*scaleOffset, true);
        StatesAttackLycan[4] = new PlayerBillboardState("AttackLycanBackward", StatesAttackLycanLength, archiveLycan, 9, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesAttackLycan[5] = new PlayerBillboardState("AttackLycanBackwardRight", StatesAttackLycanLength, archiveLycan, 8, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesAttackLycan[6] = new PlayerBillboardState("AttackLycanRight", StatesAttackLycanLength, archiveLycan, 7, 0, StatesAttackLycanOffset*scaleOffset, false);
        StatesAttackLycan[7] = new PlayerBillboardState("AttackLycanForwardRight", StatesAttackLycanLength, archiveLycan, 6, 0, StatesAttackLycanOffset*scaleOffset, false);

        StatesDeathLycan = new PlayerBillboardState[numberOrientations];
        StatesDeathLycan[0] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 10, 0, StatesDeathLycanOffset*scaleOffset, false);
        StatesDeathLycan[1] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 11, 0, StatesDeathLycanOffset*scaleOffset, true);
        StatesDeathLycan[2] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 12, 0, StatesDeathLycanOffset*scaleOffset, true);
        StatesDeathLycan[3] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 13, 0, StatesDeathLycanOffset*scaleOffset, true);
        StatesDeathLycan[4] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 14, 0, StatesDeathLycanOffset*scaleOffset, false);
        StatesDeathLycan[5] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 13, 0, StatesDeathLycanOffset*scaleOffset, false);
        StatesDeathLycan[6] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 12, 0, StatesDeathLycanOffset*scaleOffset, false);
        StatesDeathLycan[7] = new PlayerBillboardState("Death", StatesDeathLycanLength, archiveLycan, 11, 0, StatesDeathLycanOffset*scaleOffset, false);

        lengthsChanged = false;
    }

    // Start is called before the first frame update
    public void Initialize(int foot, int horse, float size, float sizeOffset, int ready, int turn, int attackString, float mirrorDecay, int pingpongAdjust, int torchFollow)
    {
        if (!IsReady)
            return;

        scale = size;
        scaleOffsetMod = sizeOffset;
        readyStance = ready;
        turnToView = turn;
        attackStrings = attackString;
        mirrorTime = mirrorDecay;
        pingpongOffset = pingpongAdjust;
        torchOffset = torchFollow;
        died = false;

        // Get component references
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (torch == null)
        {
            torch = GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.transform;
            torchPosLocalDefault = torch.localPosition;
        }

        if (foot != indexFoot || horse != indexHorse || lengthsChanged)
        {
            indexFoot = foot;
            indexHorse = horse;
            InitializeStates();
        }

        if (meshRenderer.sharedMaterial == null)
            AssignMeshAndMaterial();

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        mirrorCount = 0;
        mirrorTimer = 0;
        pingpongCount = 0;

        if (stateLast != null)
            stateCurrent = stateLast;
        else
            stateCurrent = StatesIdle;

        if (footsteps)
            DisableVanillaFootsteps();
        else
            EnableVanillaFootsteps();

        UpdateOrientation(true);
    }

    private void Update()
    {
        if (GameManager.IsGamePaused)
            return;

        if (orientationTime > 0)
        {
            if (orientationTimer <= orientationTime)
                orientationTimer += Time.deltaTime;
        }

        if (attackStrings == 1 || attackStrings == 3)
        {
            if (mirrorCount > 0 && isAnimating == null && mirrorTime > 0)
            {
                if (mirrorTimer > mirrorTime)
                {
                    mirrorCount = 0;
                    mirrorTimer = 0;
                    UpdateBillboard(frameCurrent,lastOrientation,stateCurrent);
                }
                else
                {
                    mirrorTimer += Time.deltaTime;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (GameManager.IsGamePaused)
            return;

        // Rotate to face camera in game
        if (mainCamera && Application.isPlaying)
        {
            // Rotate billboard based on camera facing
            if (FP)
                cameraPosition = transform.parent.position + (Vector3.up*0.9f);
            else
                cameraPosition = mainCamera.transform.position;
            Vector3 viewDirection = -new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
            transform.LookAt(transform.position + viewDirection);

            if (died)
                return;

            if (GameManager.Instance.PlayerDeath.DeathInProgress && !died)
            {
                died = true;
                PlayDeathAnimation();
                return;
            }

            // Orient enemy based on camera position
            UpdateOrientation();

            UpdateMaterial();

            if (isAnimating == null)
            {
                //weapon attacks
                if (GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking())
                {
                    if (GameManager.Instance.PlayerEffectManager.IsTransformedLycanthrope())
                    {
                        PlayLycanAttackAnimation();
                    }
                    else if (GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType != WeaponTypes.Bow)
                        PlayMeleeAttackAnimation();
                    else
                    {
                        if (DaggerfallUnity.Settings.BowDrawback)
                            PlayRangedAttackAnimationHold();
                        else
                            PlayRangedAttackAnimation();
                    }
                }

                //spell attacks
                if (GameManager.Instance.PlayerSpellCasting.IsPlayingAnim)
                    PlaySpellAttackAnimation();

                LoopIdleBillboard();
            }
        }
    }
    void LoopIdleBillboard()
    {
        bool isGrounded = GameManager.Instance.PlayerMotor.IsGrounded;
        bool isSpellcasting = GameManager.Instance.PlayerEffectManager.HasReadySpell;
        bool isStopped = GameManager.Instance.PlayerMotor.IsStandingStill;
        bool isSheathed = GameManager.Instance.WeaponManager.Sheathed;
        bool isUsingBow = GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType == WeaponTypes.Bow;
        bool isRiding = GameManager.Instance.PlayerMotor.IsRiding;
        bool isTransformed = GameManager.Instance.PlayerEffectManager.IsTransformedLycanthrope();
        bool isLevitating = GameManager.Instance.PlayerMotor.IsLevitating;
        bool isSwimming = GameManager.Instance.PlayerMotor.IsSwimming;

        if (isStopped && !stopped)
            stopped = true;
        else if (!isStopped && stopped)
            stopped = false;

        if (isSheathed && !sheathed)
            sheathed = true;
        else if (!isSheathed && sheathed)
            sheathed = false;

        if (isSpellcasting && !spellcasting)
            spellcasting = true;
        else if (!isSpellcasting && spellcasting)
            spellcasting = false;

        if (isUsingBow && !usingBow)
            usingBow = true;
        else if (!isUsingBow && usingBow)
            usingBow = false;

        if (isRiding && !riding)
            riding = true;
        else if (!isRiding && riding)
            riding = false;

        if (isTransformed && !transformed)
            transformed = true;
        else if (!isTransformed && transformed)
            transformed = false;

        if ((isLevitating || isSwimming) && !floating)
            floating = true;
        else if (floating)
            floating = false;

        if (stateCurrent[lastOrientation].length > 1)
        {
            float speed = 1f;

            if (GameManager.Instance.PlayerMotor.IsRunning)
                speed *= 0.5f;

            if (GameManager.Instance.PlayerMotor.IsCrouching)
                speed *= 2f;

            if (GameManager.Instance.SpeedChanger.isSneaking)
                speed *= 2f;

            if (footsteps)
            {
                if (!isStopped && isGrounded && !animating)
                {
                    int step = 2;
                    if (GameManager.Instance.PlayerMotor.IsRiding)
                        step = 4;

                    if (frameCurrent % step == 0)
                    {
                        if (!hasPlayedFootstep)
                            PlayFootstep();
                    }
                    else
                        hasPlayedFootstep = false;
                }
            }

            if (frameTimer > frameTime * speed)
            {
                if (frameCurrent < stateCurrent[lastOrientation].length - 1)
                    frameCurrent++;
                else
                    frameCurrent = 0;

                // Assign imported texture
                if (frameCurrent > stateCurrent[lastOrientation].length - 1)
                    frameCurrent = 0;

                UpdateBillboard(frameCurrent, lastOrientation, stateCurrent);
                frameTimer = 0;
            }
            else
            {
                frameTimer += Time.deltaTime;
            }
        }

        if (isTransformed)
        {
            if (isStopped)
            {
                if (!isSheathed)
                    stateCurrent = StatesReadyLycan;
                else
                    stateCurrent = StatesIdleLycan;
            }
            else
                stateCurrent = StatesMoveLycan;
        }
        else
        {
            if (isStopped)
            {
                if (isRiding)
                    stateCurrent = StatesIdleHorse;
                else
                {
                    if (readyStance > 0)
                    {
                        if (isSpellcasting)
                            stateCurrent = StatesReadySpell;
                        else if (!isSheathed)
                        {
                            if (isUsingBow)
                                stateCurrent = StatesReadyRanged;
                            else
                                stateCurrent = StatesReadyMelee;
                        }
                        else
                            stateCurrent = StatesIdle;
                    }
                    else
                        stateCurrent = StatesIdle;
                }
            }
            else
            {
                if (isRiding)
                    stateCurrent = StatesMoveHorse;
                else
                {
                    if (readyStance > 1)
                    {
                        if (isSpellcasting)
                            stateCurrent = StatesMoveSpell;
                        else if (!isSheathed)
                        {
                            if (isUsingBow)
                                stateCurrent = StatesMoveRanged;
                            else
                                stateCurrent = StatesMoveMelee;
                        }
                        else
                            stateCurrent = StatesMove;
                    }
                    else
                        stateCurrent = StatesMove;
                }
            }
        }

        if (stateLast != stateCurrent || (animating && isAnimating == null))
            UpdateBillboard(frameCurrent, lastOrientation, stateCurrent);

        if (isAnimating != null && !animating)
            animating = true;
        else if (isAnimating == null && animating)
            animating = false;

        stateLast = stateCurrent;
    }

    void UpdateOrientation(bool force = false)
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;

        if (orientationTime > 0)
        {
            if (orientationTimer > orientationTime)
                orientationTimer = 0;
            else
                return;
        }

        // Get direction normal to camera, ignore y axis
        Vector3 dir = Vector3.Normalize(
            new Vector3(cameraPosition.x, 0, cameraPosition.z) -
            new Vector3(transform.parent.position.x, 0, transform.parent.position.z));

        //handle vertical movement
        Vector3 currentMoveDirection = GameManager.Instance.PlayerMotor.MoveDirection;
        currentMoveDirection.y = 0;

        if (currentMoveDirection == Vector3.zero)
        {
            if (lastMoveDirection == Vector3.zero)
                currentMoveDirection = mainCamera.transform.forward;
            else
                currentMoveDirection = lastMoveDirection;
        }

        // Get parent forward normal, ignore y axis
        Vector3 parentForward = mainCamera.transform.forward;
        if (!floating)
        {
            if (turnToView > 2) //always turn to view
            {
                parentForward = mainCamera.transform.forward;
            }
            else if (turnToView > 1) //turn to view when weapon readied or animating
            {
                if (isAnimating == null)  //if not animating
                {
                    if (sheathed && !spellcasting)
                    {
                        if (!stopped)
                        {
                            parentForward = currentMoveDirection;
                        }
                        else
                            parentForward = lastMoveDirection;
                    }
                    else
                        parentForward = mainCamera.transform.forward;
                }
                else //turn if animating
                    parentForward = mainCamera.transform.forward;
            }
            else if (turnToView > 0) //turn to view when animating only
            {
                if (isAnimating == null)  //if not animating
                {
                    if (!stopped)
                    {
                        parentForward = currentMoveDirection;
                    }
                    else
                        parentForward = lastMoveDirection;
                }
                else //turn if animating
                    parentForward = mainCamera.transform.forward;
            }
            else //never turn to view
            {
                if (!stopped)
                {
                    parentForward = currentMoveDirection;
                }
                else
                    parentForward = lastMoveDirection;
            }
        }

        parentForward.y = 0;

        lastMoveDirection = parentForward;

        currentAngle = Vector3.SignedAngle(dir, parentForward,Vector3.up);

        // Facing index
        int orientation = -Mathf.RoundToInt(currentAngle / anglePerOrientation);
        // Wrap values to 0 .. numberOrientations-1
        orientation = (orientation + numberOrientations) % numberOrientations;

        if (FP)
        {
            if (orientation == 0)
                orientation = 4;
        }

        // Change enemy to this orientation
        if (orientation != lastOrientation || force || meshRenderer.material.mainTexture == null)
        {
            UpdateBillboard(frameCurrent, orientation, stateCurrent);
        }

        if (torch != null)
        {
            if (torchOffset > 0 && !FP)
                torch.transform.position = transform.position + (Vector3.up * 0.45f) + (lastMoveDirection.normalized * 0.5f);
            /*else
                torch.transform.localPosition = torchPosLocalDefault;*/
        }
    }

    /// <summary>
    /// Sets enemy orientation index.
    /// </summary>
    /// <param name="orientation">New orientation index.</param>
    private void UpdateBillboard(int frame, int orientation, PlayerBillboardState[] states)
    {
        // Get mesh filter
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        bool flip = states[orientation].flipped;

        if (attackStrings == 1 || attackStrings == 3)
        {
            if ((states == StatesIdle || states == StatesIdleLycan || states == StatesMove || states == StatesMoveMelee || states == StatesMoveSpell || states == StatesMoveLycan || states == StatesReadyMelee || states == StatesReadySpell || states == StatesReadyLycan || states == StatesAttackMelee || states == StatesAttackSpell || states == StatesAttackLycan) && (orientation == 0 || orientation == 4))
            {
                if (mirrorCount % 2 != 0)
                    flip = !flip;
            }
        }

        // Assign imported texture
        if (frame > states[orientation].length - 1)
            frame = states[orientation].length - 1;

        Rect rect = states[orientation].GetRect(frame,flip);

        Vector2 size = rect.size*sizeMod;

        transform.localPosition = rect.position;

        // Set mesh scale for this state
        transform.localScale = new Vector3(size.x, size.y, 1);

        // Update Record/Frame texture
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.mainTexture = states[orientation].frames[frame];

        frameCurrent = frame;

        // Update UVs on mesh
        Vector2[] uvs = new Vector2[4];
        if (flip)
        {
            uvs[0] = new Vector2(1, 1);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(1, 0);
            uvs[3] = new Vector2(0, 0);
        }
        else
        {
            uvs[0] = new Vector2(0, 1);
            uvs[1] = new Vector2(1, 1);
            uvs[2] = new Vector2(0, 0);
            uvs[3] = new Vector2(1, 0);
        }
        meshFilter.sharedMesh.uv = uvs;

        // Assign new orientation
        lastOrientation = orientation;
    }

    /// <summary>
    /// Creates mesh and material for this enemy.
    /// </summary>
    /// <param name="dfUnity">DaggerfallUnity singleton. Required for content readers and settings.</param>
    /// <param name="archive">Texture archive index derived from type and gender.</param>
    private void AssignMeshAndMaterial()
    {
        // Get mesh filter
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        // Vertices for a 1x1 unit quad
        // This is scaled to correct size depending on facing and orientation
        float hx = 0.5f, hy = 0.5f;
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(hx, hy, 0);
        vertices[1] = new Vector3(-hx, hy, 0);
        vertices[2] = new Vector3(hx, -hy, 0);
        vertices[3] = new Vector3(-hx, -hy, 0);

        // Indices
        int[] indices = new int[6]
        {
                0, 1, 2,
                3, 2, 1,
        };

        // Normals
        Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
        Vector3[] normals = new Vector3[4];
        normals[0] = normal;
        normals[1] = normal;
        normals[2] = normal;
        normals[3] = normal;

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = string.Format("MobileEnemyMesh");
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.normals = normals;

        // Assign mesh
        meshFilter.sharedMesh = mesh;

        // Create material
        Material material = MakeBillboardMaterial(null);

        // Set new enemy material
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        shaderNormal = meshRenderer.sharedMaterial.shader;
        shaderGhost = Shader.Find(MaterialReader._DaggerfallGhostShaderName);
    }

    void UpdateMaterial()
    {
        bool isInvisible = GameManager.Instance.PlayerEntity.IsInvisible;
        bool isShadow = GameManager.Instance.PlayerEntity.IsAShade;
        bool isBlending = GameManager.Instance.PlayerEntity.IsBlending;

        lastColor = meshRenderer.sharedMaterial.GetColor("_Color");

        if (isInvisible || isShadow || isBlending)
        {
            if (meshRenderer.sharedMaterial.shader != shaderGhost)
            {
                meshRenderer.sharedMaterial.shader = shaderGhost;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (isInvisible)
            {
                if (lastColor.a != 0.4f)
                {
                    Color currentColor = Color.white;
                    currentColor.a = 0.4f;
                    meshRenderer.sharedMaterial.SetColor("_Color", currentColor);
                }
            }
            else if (isShadow)
            {
                if (lastColor.a != 0.6f)
                {
                    Color currentColor = Color.black;
                    currentColor.a = 0.6f;
                    meshRenderer.sharedMaterial.SetColor("_Color", currentColor);
                }
            }
            else
            {
                if (lastColor.a != 0.8f)
                {
                    Color currentColor = Color.white;
                    currentColor.a = 0.8f;
                    meshRenderer.sharedMaterial.SetColor("_Color", currentColor);
                }
            }
        }
        else
        {
            if (meshRenderer.sharedMaterial.shader != shaderNormal)
            {
                meshRenderer.sharedMaterial.shader = shaderNormal;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (lastColor.a != 1)
            {
                Color currentColor = Color.white;
                meshRenderer.sharedMaterial.SetColor("_Color", currentColor);
            }
        }
    }

    void PlayMeleeAttackAnimation()
    {
        if (riding || isAnimating != null)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        bool pingpong = false;

        if (attackStrings == 3)
        {
            //if (UnityEngine.Random.value < 0.33f)
            if (pingpongCount % 4 == 0)
                pingpong = true;
        }
        else if (attackStrings == 2)
            pingpong = true;

        float speed;

        if (pingpong)
        {
            speed = GetMeleeAnimTickTime((((StatesAttackMelee[lastOrientation].length/2)+pingpongOffset)*2)-1);
            isAnimating = PlayAnimationPingPongCoroutine(StatesAttackMelee, speed);
        }
        else
        {
            speed = GetMeleeAnimTickTime(StatesAttackMelee[lastOrientation].length);
            isAnimating = PlayAnimationCoroutine(StatesAttackMelee, speed);
        }

        if (EyeOfTheBeholder.Instance != null)
            EyeOfTheBeholder.Instance.HasSwungWeapon = true;

        if (StatesAttackSpell.Length > 1)
            StartCoroutine(isAnimating);
        else
            isAnimating = null;
    }

    void PlayRangedAttackAnimation()
    {
        if (riding || isAnimating != null)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        isAnimating = PlayAnimationCoroutine(StatesAttackRanged, GameManager.classicUpdateInterval * 2);

        if (EyeOfTheBeholder.Instance != null)
            EyeOfTheBeholder.Instance.HasFiredMissile = true;

        if (StatesAttackSpell.Length > 1)
            StartCoroutine(isAnimating);
        else
            isAnimating = null;
    }

    void PlayRangedAttackAnimationHold()
    {
        if (riding || isAnimating != null)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        isAnimating = PlayAnimationHoldCoroutine(StatesAttackRanged, GameManager.classicUpdateInterval * 2,InputManager.Actions.SwingWeapon,InputManager.Actions.ActivateCenterObject,10);

        StartCoroutine(isAnimating);
    }

    void PlaySpellAttackAnimation()
    {
        if (riding || isAnimating != null)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        isAnimating = PlayAnimationCoroutine(StatesAttackSpell, GameManager.classicUpdateInterval * 2);

        if (EyeOfTheBeholder.Instance != null)
            EyeOfTheBeholder.Instance.HasFiredMissile = true;

        if (StatesAttackSpell.Length > 1)
            StartCoroutine(isAnimating);
        else
            isAnimating = null;
    }

    void PlayLycanAttackAnimation()
    {
        if (riding || isAnimating != null)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        isAnimating = PlayAnimationCoroutine(StatesAttackLycan, GameManager.classicUpdateInterval * 2);

        if (EyeOfTheBeholder.Instance != null)
            EyeOfTheBeholder.Instance.HasSwungWeapon = true;

        if (StatesAttackSpell.Length > 1)
            StartCoroutine(isAnimating);
        else
            isAnimating = null;
    }

    void PlayDeathAnimation()
    {
        if (riding)
            return;

        if (isAnimating != null)
        {
            StopCoroutine(isAnimating);
            isAnimating = null;
        }

        if (GameManager.Instance.PlayerEffectManager.IsTransformedLycanthrope())
            isAnimating = PlayAnimationCoroutine(StatesDeathLycan, GameManager.classicUpdateInterval * 4, true);
        else
            isAnimating = PlayAnimationCoroutine(StatesDeath, GameManager.classicUpdateInterval * 4, true);


        if (StatesAttackSpell.Length > 1)
            StartCoroutine(isAnimating);
        else
            isAnimating = null;
    }

    IEnumerator PlayAnimationCoroutine(PlayerBillboardState[] states, float interval, bool freeze = false)
    {
        int animFrameCurrent = 0;
        while (animFrameCurrent < states[lastOrientation].length)
        {
            UpdateBillboard(animFrameCurrent,lastOrientation,states);
            animFrameCurrent++;
            yield return new WaitForSeconds(interval);
        }

        if (attackStrings == 1 || attackStrings == 3)
        {
            if (states == StatesAttackMelee || states == StatesAttackLycan)
            {
                mirrorTimer = 0;
                mirrorCount++;
            }
            else
                mirrorCount = 0;
        }

        if (attackStrings == 3)
            pingpongCount++;

        isAnimating = null;

        if (!freeze)
            UpdateOrientation(true);
    }

    IEnumerator PlayAnimationPingPongCoroutine(PlayerBillboardState[] states, float interval, bool freeze = false)
    {
        int animFrameCurrent = 0;

        while (animFrameCurrent < (states[lastOrientation].length/2)+pingpongOffset)
        {
            UpdateBillboard(animFrameCurrent, lastOrientation, states);
            animFrameCurrent++;
            yield return new WaitForSeconds(interval);
        }

        animFrameCurrent--;

        while (animFrameCurrent > 0)
        {
            UpdateBillboard(animFrameCurrent, lastOrientation, states);
            animFrameCurrent--;
            yield return new WaitForSeconds(interval);
        }

        if (attackStrings == 3)
            pingpongCount++;

        isAnimating = null;

        if (!freeze)
            UpdateOrientation(true);
    }

    IEnumerator PlayAnimationHoldCoroutine(PlayerBillboardState[] states, float interval, InputManager.Actions actionTrigger, InputManager.Actions actionCancel, int maxDuration)
    {
        int animFrameCurrent = states[lastOrientation].length-1;
        bool trigger = false;

        //play in reverse
        while (!trigger)
        {
            while (animFrameCurrent > 0) {
                animFrameCurrent--;
                UpdateBillboard(animFrameCurrent, lastOrientation, states);
                yield return new WaitForSeconds(interval);
            }

            if (InputManager.Instance.HasAction(actionTrigger))
            {
                if (InputManager.Instance.HasAction(actionCancel))
                {
                    isAnimating = null;
                    UpdateOrientation(true);
                    yield break;
                }

                UpdateBillboard(animFrameCurrent, lastOrientation, states);
                yield return new WaitForEndOfFrame();
            }
            else
            {
                trigger = true;
                if (EyeOfTheBeholder.Instance != null)
                    EyeOfTheBeholder.Instance.HasFiredMissile = true;
            }
        }

        animFrameCurrent = 0;

        //play normally
        while (animFrameCurrent < states[lastOrientation].length)
        {
            UpdateBillboard(animFrameCurrent, lastOrientation, states);
            animFrameCurrent++;
            yield return new WaitForSeconds(interval);
        }

        isAnimating = null;

        UpdateOrientation(true);
    }

    float GetMeleeAnimTickTime(int length)
    {
        float baseTickTime = FormulaHelper.GetMeleeWeaponAnimTime(GameManager.Instance.PlayerEntity, GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType, GameManager.Instance.WeaponManager.ScreenWeapon.WeaponHands);
        float baseAnimTime = baseTickTime * 5;

        return baseAnimTime / length;
    }
    private static Material MakeBillboardMaterial(string renderMode = null)
    {
        // Parse blendMode from string or use Cutout if no custom blendMode specified
        MaterialReader.CustomBlendMode blendMode =
            renderMode != null && Enum.IsDefined(customBlendModeType, renderMode) ?
            (MaterialReader.CustomBlendMode)Enum.Parse(customBlendModeType, renderMode) :
            MaterialReader.CustomBlendMode.Cutout;

        // Use Daggerfall/Billboard material for standard cutout billboards or create a Standard material if using any other custom blendMode
        if (blendMode == MaterialReader.CustomBlendMode.Cutout)
            return MaterialReader.CreateBillboardMaterial();
        else
            return MaterialReader.CreateStandardMaterial(blendMode);
    }

    void DisableVanillaFootsteps()
    {
        PlayerFootsteps playerFootsteps = GameManager.Instance.PlayerObject.GetComponent<PlayerFootsteps>();

        playerFootsteps.FootstepSoundDungeon1 = SoundClips.None;
        playerFootsteps.FootstepSoundDungeon2 = SoundClips.None;
        playerFootsteps.FootstepSoundOutside1 = SoundClips.None;
        playerFootsteps.FootstepSoundOutside2 = SoundClips.None;
        playerFootsteps.FootstepSoundSnow1 = SoundClips.None;
        playerFootsteps.FootstepSoundSnow2 = SoundClips.None;
        playerFootsteps.FootstepSoundBuilding1 = SoundClips.None;
        playerFootsteps.FootstepSoundBuilding2 = SoundClips.None;
        playerFootsteps.FootstepSoundShallow = SoundClips.None;
        playerFootsteps.FootstepSoundSubmerged = SoundClips.None;
    }

    void EnableVanillaFootsteps()
    {
        PlayerFootsteps playerFootsteps = GameManager.Instance.PlayerObject.GetComponent<PlayerFootsteps>();

        playerFootsteps.FootstepSoundDungeon1 = SoundClips.PlayerFootstepStone1;
        playerFootsteps.FootstepSoundDungeon2 = SoundClips.PlayerFootstepStone2;
        playerFootsteps.FootstepSoundOutside1 = SoundClips.PlayerFootstepOutside1;
        playerFootsteps.FootstepSoundOutside2 = SoundClips.PlayerFootstepOutside2;
        playerFootsteps.FootstepSoundSnow1 = SoundClips.PlayerFootstepSnow1;
        playerFootsteps.FootstepSoundSnow2 = SoundClips.PlayerFootstepSnow2;
        playerFootsteps.FootstepSoundBuilding1 = SoundClips.PlayerFootstepWood1;
        playerFootsteps.FootstepSoundBuilding2 = SoundClips.PlayerFootstepWood2;
        playerFootsteps.FootstepSoundShallow = SoundClips.SplashSmallLow;
        playerFootsteps.FootstepSoundSubmerged = SoundClips.SplashSmall;
    }

    void PlayFootstep()
    {
        //this condition helps prevent making a nuisance footstep noise when the player first
        //loads a save, or into an interior or exterior location
        /*if (GameManager.Instance.SaveLoadManager.LoadInProgress || GameManager.Instance.StreamingWorld.IsRepositioningPlayer)
        {
            ignoreLostGrounding = true;
            return;
        }*/

        AudioSource audioSource = GameManager.Instance.PlayerObject.GetComponent<AudioSource>();
        DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();

        PlayerFootsteps playerFootsteps = GameManager.Instance.PlayerObject.GetComponent<PlayerFootsteps>();
        PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
        PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;

        DaggerfallDateTime.Seasons playerSeason = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue;
        int playerClimateIndex = GameManager.Instance.PlayerGPS.CurrentClimateIndex;

        // Get player inside flag
        // Can only do this when PlayerEnterExit is available, otherwise default to true
        bool playerInside = (playerEnterExit == null) ? true : playerEnterExit.IsPlayerInside;
        bool playerInBuilding = (playerEnterExit == null) ? false : playerEnterExit.IsPlayerInsideBuilding;

        // Play splash footsteps whether player is walking on or swimming in exterior water
        bool playerOnExteriorWater = (GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.Swimming || GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.WaterWalking);

        bool playerOnExteriorPath = GameManager.Instance.PlayerMotor.OnExteriorPath;
        bool playerOnStaticGeometry = GameManager.Instance.PlayerMotor.OnExteriorStaticGeometry;

        SoundClips currentFootstepSound1;
        SoundClips currentFootstepSound2;

        // Change footstep sounds between winter/summer variants, when player enters/exits an interior space, or changes between path, water, or other outdoor ground
        if (!playerInside && !playerOnStaticGeometry)
        {
            if (playerSeason == DaggerfallDateTime.Seasons.Winter && !WeatherManager.IsSnowFreeClimate(playerClimateIndex))
            {
                currentFootstepSound1 = SoundClips.PlayerFootstepSnow1;
                currentFootstepSound2 = SoundClips.PlayerFootstepSnow2;
            }
            else
            {
                currentFootstepSound1 = SoundClips.PlayerFootstepOutside1;
                currentFootstepSound2 = SoundClips.PlayerFootstepOutside2;
            }
        }
        else if (playerInBuilding)
        {
            currentFootstepSound1 = SoundClips.PlayerFootstepWood1;
            currentFootstepSound2 = SoundClips.PlayerFootstepWood2;
        }
        else // in dungeon
        {
            currentFootstepSound1 = SoundClips.PlayerFootstepStone1;
            currentFootstepSound2 = SoundClips.PlayerFootstepStone2;
        }

        // walking on water tile
        if (playerOnExteriorWater)
        {
            currentFootstepSound1 = SoundClips.SplashSmall;
            currentFootstepSound2 = SoundClips.SplashSmall;
        }

        // walking on path tile
        if (playerOnExteriorPath)
        {
            currentFootstepSound1 = SoundClips.PlayerFootstepStone1;
            currentFootstepSound2 = SoundClips.PlayerFootstepStone2;
        }

        // Use water sounds if in dungeon water
        if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon && playerEnterExit.blockWaterLevel != 10000)
        {
            // In water, deep depth
            if ((currentFootstepSound1 != SoundClips.SplashSmall) && playerEnterExit.IsPlayerSwimming)
            {
                currentFootstepSound1 = SoundClips.SplashSmall;
                currentFootstepSound2 = SoundClips.SplashSmall;
            }
            // In water, shallow depth
            else if ((currentFootstepSound1 != SoundClips.SplashSmallLow) && !playerEnterExit.IsPlayerSwimming && (playerMotor.transform.position.y - 0.57f) < (playerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale))
            {
                currentFootstepSound1 = SoundClips.SplashSmallLow;
                currentFootstepSound2 = SoundClips.SplashSmallLow;
            }
        }

        // Not in water, reset footsteps to normal
        if ((!playerOnExteriorWater)
            && (currentFootstepSound1 == SoundClips.SplashSmall || currentFootstepSound1 == SoundClips.SplashSmallLow)
            && (playerEnterExit.blockWaterLevel == 10000 || (playerMotor.transform.position.y - 0.95f) >= (playerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale)))
        {
            currentFootstepSound1 = SoundClips.PlayerFootstepStone1;
            currentFootstepSound2 = SoundClips.PlayerFootstepStone2;
        }

        // Check whether player is on foot and abort playing footsteps if not.
        if (playerMotor.IsLevitating || !GameManager.Instance.TransportManager.IsOnFoot && playerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.None)
        {
            return;
        }

        if (footstepSound1 != currentFootstepSound1 || footstepSound2 != currentFootstepSound2)
        {
            footstepSound1 = currentFootstepSound1;
            footstepSound2 = currentFootstepSound2;

            footstep1 = dfAudioSource.GetAudioClip((int)footstepSound1);
            footstep2 = dfAudioSource.GetAudioClip((int)footstepSound2);
        }

        // Check if player is grounded
        // Note: In classic, submerged "footstep" sound is only played when walking on the floor while in the water, but it sounds like a swimming sound
        // and when outside is played while swimming at the water's surface, so it seems better to play it all the time while submerged in water.
        if (!playerMotor.IsSwimming)
        {
            float povMod = 2;

            if (FP)
                povMod = 1;

            if (!footstepAlt)
                audioSource.PlayOneShot(footstep1, playerFootsteps.FootstepVolumeScale * DaggerfallUnity.Settings.SoundVolume * povMod);
            else
                audioSource.PlayOneShot(footstep2, playerFootsteps.FootstepVolumeScale * DaggerfallUnity.Settings.SoundVolume * povMod);

            footstepAlt = !footstepAlt;
        }

        hasPlayedFootstep = true;
    }
}
