using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField]
    private List<SlotImage> images;     //class to store total images
    [SerializeField]
    private List<SlotImage> Tempimages;     //class to store the result matrix

    [Header("Slots Elements")]
    [SerializeField]
    private LayoutElement[] Slot_Elements;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;

    private Dictionary<int, string> y_string = new Dictionary<int, string>();

    [Header("Buttons")]
    [SerializeField]
    private Button SlotStart_Button;
    [SerializeField]
    private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField]
    private Button MaxBet_Button;
    [SerializeField]
    private Button BetPlus_Button;
    [SerializeField]
    private Button BetMinus_Button;
    [SerializeField] private Button StopSpin_Button;
    [SerializeField] private Button Turbo_Button;

    [Header("Animated Sprites")]
    [SerializeField]
    private Sprite[] Ten_Sprite;
    [SerializeField]
    private Sprite[] A_Sprite;
    [SerializeField]
    private Sprite[] J_Sprite;
    [SerializeField]
    private Sprite[] K_Sprite;
    [SerializeField]
    private Sprite[] Q_Sprite;
    [SerializeField]
    private Sprite[] Ankh_Sprite;
    [SerializeField]
    private Sprite[] Eye_Sprite;
    [SerializeField]
    private Sprite[] Lotus_Sprite;
    [SerializeField]
    private Sprite[] Shen_Sprite;
    [SerializeField]
    private Sprite[] Wick_Sprite;
    [SerializeField]
    private Sprite[] FreeSpin_Sprite;
    [SerializeField]
    private Sprite[] Jackpot_Sprite;
    [SerializeField]
    private Sprite[] Scatter_Sprite;
    [SerializeField]
    private Sprite[] Wild_Sprite;

    [Header("Miscellaneous UI")]
    [SerializeField]
    private TMP_Text Balance_text;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text LineBet_text;
    [SerializeField]
    private TMP_Text TotalWin_text;


    [Header("Audio Management")]
    [SerializeField]
    private AudioController audioController;

    [SerializeField]
    private UIManager uiManager;

    [Header("Free Spins Board")]
    [SerializeField]
    private GameObject FSBoard_Object;
    [SerializeField]
    private TMP_Text FSNum_text;

    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField]
    private GameObject[] FireAnim_Objects;

    [SerializeField]
    private PayoutCalculation PayCalculator;

    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;

    [SerializeField]
    private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 

    [SerializeField]
    private SocketIOManager SocketManager;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine FreeSpinRoutine = null;
    private Coroutine tweenroutine;

    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool IsSpinning = false;
    internal bool CheckPopups = false;
    private bool CheckSpinAudio = false;

    internal int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    protected int Lines = 20;

    private bool StopSpinToggle;
    private bool IsTurboOn;
    internal bool WasAutoSpinOn = false;
    private float SpinDelay = 0.2f;
    private Sprite turboOriginalSprite;
    private Tween ScoreTween;

    [SerializeField]
    private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing
    private int numberOfSlots = 5;          //number of columns


    private void Start()
    {
        IsAutoSpin = false;

        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (BetPlus_Button) BetPlus_Button.onClick.RemoveAllListeners();
        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.RemoveAllListeners();
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);


        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
        if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); if (audioController) audioController.PlayButtonAudio(); });

        if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

        if (FSBoard_Object) FSBoard_Object.SetActive(false);

        tweenHeight = (15 * IconSizeFactor) - 280;
        turboOriginalSprite = Turbo_Button.GetComponent<Image>().sprite;
    }

    #region Autospin
    private void AutoSpin()
    {
        if (!IsAutoSpin)
        {

            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());

        }
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            if (audioController) audioController.PlayButtonAudio();
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }

    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }

    void TurboToggle()
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IsTurboOn)
        {
            IsTurboOn = false;
            Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
            Turbo_Button.image.sprite = turboOriginalSprite;
            //Turbo_Button.image.sprite = TurboToggleSprites[0];
            //Turbo_Button.image.color = new Color(0.86f, 0.86f, 0.86f, 1);
        }
        else
        {
            IsTurboOn = true;
            Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
            //Turbo_Button.image.color = new Color(1, 1, 1, 1);
        }
    }
    #endregion

    #region FreeSpin
    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            if (FSNum_text) FSNum_text.text = spins.ToString();
            if (FSBoard_Object) FSBoard_Object.SetActive(true);
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        while (i < spinchances)
        {
            uiManager.FreeSpins--;
            if (FSNum_text) FSNum_text.text = uiManager.FreeSpins.ToString();

            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
            i++;
            //if (FSNum_text) FSNum_text.text = (spinchances - i).ToString();
        }
        if (FSBoard_Object) FSBoard_Object.SetActive(false);
        if (WasAutoSpinOn)
        {
            AutoSpin();
        }
        else
        {
            ToggleButtonGrp(true);
        }
        IsFreeSpin = false;
    }
    #endregion

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
        }
    }

    #region LinesCalculation
    //Fetch Lines from backend
    internal void FetchLines(string LineVal, int count)
    {
        y_string.Add(count + 1, LineVal);
    }

    //Generate Static Lines from button hovers
    internal void GenerateStaticLine(TMP_Text LineID_Text)
    {
        DestroyStaticLine();
        int LineID = 1;
        try
        {
            LineID = int.Parse(LineID_Text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Exception while parsing " + e.Message);
        }
        List<int> y_points = null;
        y_points = y_string[LineID]?.Split(',')?.Select(Int32.Parse)?.ToList();
        PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count, true);
    }

    //Destroy Static Lines from button hovers
    internal void DestroyStaticLine()
    {
        PayCalculator.ResetStaticLine();
    }
    #endregion

    private void MaxBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        BetCounter = SocketManager.InitialData.bets.Count - 1;
        if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
        // CompareBalance();
    }

    private void ChangeBet(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter >= SocketManager.InitialData.bets.Count)
            {
                BetCounter = 0; // Loop back to the first bet
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = SocketManager.InitialData.bets.Count - 1; // Loop to the last bet
            }
        }
        if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
        // CompareBalance();
    }


    //just for testing purposes delete on production
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && SlotStart_Button.interactable)
    //    {
    //        StartSlots();
    //    }
    //}

    #region InitialFunctions
    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }

    internal void SetInitialUI()
    {
        //        Debug.Log("AshuTest:   --------" + SocketManager.InitialData.bets.Count + "******" + SocketManager.InitialData.bets[BetCounter]);
        BetCounter = 0;
        if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
        if (TotalWin_text) TotalWin_text.text = "0.00";
        if (Balance_text) Balance_text.text = SocketManager.PlayerData.balance.ToString("f2");
        currentBalance = SocketManager.PlayerData.balance;
        currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
        CompareBalance();
        uiManager.InitialiseUIData(SocketManager.UIData.paylines);
    }
    #endregion

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            case 5:
                for (int i = 0; i < Wick_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Wick_Sprite[i]);
                }
                break;
            case 6:
                for (int i = 0; i < Shen_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Shen_Sprite[i]);
                }
                break;
            case 7:
                for (int i = 0; i < Eye_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Eye_Sprite[i]);
                }
                break;
            case 8:
                for (int i = 0; i < Ankh_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Ankh_Sprite[i]);
                }
                break;
            case 9:
                for (int i = 0; i < Lotus_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Lotus_Sprite[i]);
                }
                break;
            case 0:
                for (int i = 0; i < A_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(A_Sprite[i]);
                }
                break;
            case 1:
                for (int i = 0; i < K_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(K_Sprite[i]);
                }
                break;
            case 2:
                for (int i = 0; i < J_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(J_Sprite[i]);
                }
                break;
            case 3:
                for (int i = 0; i < Q_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Q_Sprite[i]);
                }
                break;
            case 4:
                for (int i = 0; i < Ten_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Ten_Sprite[i]);
                }
                break;
            case 10:
                for (int i = 0; i < Wild_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Wild_Sprite[i]);
                }
                break;
            case 11:
                for (int i = 0; i < Scatter_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Scatter_Sprite[i]);
                }
                break;
            case 12:
                for (int i = 0; i < Jackpot_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Jackpot_Sprite[i]);
                }
                break;
            case 13:
                for (int i = 0; i < FreeSpin_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(FreeSpin_Sprite[i]);
                }
                break;
        }
    }

    #region SlotSpin
    //starts the spin process
    private void StartSlots(bool autoSpin = false)
    {

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }
        }
        if (audioController) audioController.PlaySpinButtonAudio();
        if (TotalWin_text) TotalWin_text.text = "0.000";

        WinningsAnim(false);
        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }
        PayCalculator.ResetLines();
        tweenroutine = StartCoroutine(TweenRoutine());
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine()
    {
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }
        if (audioController) audioController.PlayWLAudio("spin");
        CheckSpinAudio = true;

        IsSpinning = true;

        ToggleButtonGrp(false);

        if (!IsTurboOn && !IsAutoSpin)
        {
            StopSpin_Button.gameObject.SetActive(true);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }
        SocketManager.AccumulateResult(BetCounter);

        yield return new WaitUntil(() => SocketManager.isResultdone);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int resultNum = int.Parse(SocketManager.ResultData.matrix[i][j]);
                // print("resultNum: " + resultNum);
                // print("image loc: " + j + " " + i);
                PopulateAnimationSprites(Tempimages[j].slotImages[i].GetComponent<ImageAnimation>(), resultNum);
                Tempimages[j].slotImages[i].sprite = myImages[resultNum];
            }
        }

        if (IsTurboOn)                                                      // changes
        {

            yield return new WaitForSeconds(0.1f);
            StopSpinToggle = true;
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (StopSpinToggle)
                {
                    break;
                }
            }
            StopSpin_Button.gameObject.SetActive(false);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i, StopSpinToggle);
        }
        StopSpinToggle = false;
        if (SocketManager.ResultData.payload.winAmount > 0)
        {
            SpinDelay = 2f;
        }
        else
        {
            SpinDelay = 0.2f;
        }
        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();

        if (SocketManager.ResultData.payload.winAmount > 0)
        {
            List<int> winLine = new();
            foreach (var item in SocketManager.ResultData.payload.wins)
            {
                winLine.Add(item.line);
            }
            CheckPayoutLineBackend(winLine);
        }
        CheckForFeaturesAnimation();

        CheckPopups = true;
        ScoreTween?.Kill();
        if (TotalWin_text) TotalWin_text.text = SocketManager.ResultData.payload.winAmount.ToString("f3");

        if (Balance_text) Balance_text.text = SocketManager.PlayerData.balance.ToString("f3");

        currentBalance = SocketManager.PlayerData.balance;

        if (SocketManager.ResultData.jackpot.isTriggered)
        {
            uiManager.PopulateWin(4, SocketManager.ResultData.jackpot.amount);
            yield return new WaitUntil(() => !CheckPopups);
            CheckPopups = true;
        }
        CheckWinPopups();

        yield return new WaitUntil(() => !CheckPopups);
        if (!IsAutoSpin && !IsFreeSpin)
        {
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            //  yield return new WaitForSeconds(2f);
            IsSpinning = false;
        }
        if (SocketManager.ResultData.freeSpin.isFreeSpin)
        {
            if (IsFreeSpin)
            {
                IsFreeSpin = false;
                if (FreeSpinRoutine != null)
                {
                    StopCoroutine(FreeSpinRoutine);
                    FreeSpinRoutine = null;
                }
            }
            uiManager.FreeSpinProcess((int)SocketManager.ResultData.freeSpin.count);
            if (IsAutoSpin)
            {
                WasAutoSpinOn = true;

                StopAutoSpin();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        ScoreTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f2");
        });
    }

    internal void CheckWinPopups()
    {
        if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 10 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 15)
        {
            uiManager.PopulateWin(1, SocketManager.ResultData.payload.winAmount);
        }
        else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 15 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 20)
        {
            uiManager.PopulateWin(2, SocketManager.ResultData.payload.winAmount);
        }
        else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 20)
        {
            uiManager.PopulateWin(3, SocketManager.ResultData.payload.winAmount);
        }
        else if (SocketManager.ResultData.jackpot.isTriggered)
        {
            uiManager.PopulateWin(4, SocketManager.ResultData.payload.winAmount);
        }
        else if (SocketManager.ResultData.scatter.amount > 0)
        {
            uiManager.PopulateWin(3, SocketManager.ResultData.payload.winAmount);
        }
        else
        {
            CheckPopups = false;
        }
    }

    //generate the payout lines generated 
    private void CheckPayoutLineBackend(List<int> LineId, double jackpot = 0)
    {
        List<int> y_points = null;
        if (LineId.Count > 0)
        {


            for (int i = 0; i < LineId.Count; i++)
            {
                y_points = y_string[LineId[i] + 1]?.Split(',')?.Select(Int32.Parse)?.ToList();
                PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count);
            }

            if (jackpot > 0)
            {
                if (audioController) audioController.PlayWLAudio("win");
            }

            else
            {
                if (audioController) audioController.PlayWLAudio("win");
                List<KeyValuePair<int, int>> coords = new();
                for (int j = 0; j < LineId.Count; j++)
                {
                    for (int k = 0; k < SocketManager.ResultData.payload.wins[j].positions.Count; k++)
                    {
                        int rowIndex = SocketManager.InitialData.lines[LineId[j]][k];
                        int columnIndex = k;
                        coords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
                    }
                }

                foreach (var coord in coords)
                {
                    int rowIndex = coord.Key;
                    int columnIndex = coord.Value;
                    StartGameAnimation(Tempimages[columnIndex].slotImages[rowIndex].gameObject);
                }
            }
            WinningsAnim(true);
        }
        else
        {

            //if (audioController) audioController.PlayWLAudio("lose");
            if (audioController) audioController.StopWLAaudio();
        }
        CheckSpinAudio = false;
    }

    private void WinningsAnim(bool IsStart)
    {
        if (IsStart)
        {
            WinTween = TotalWin_text.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
            foreach (GameObject e in FireAnim_Objects)
            {
                e.SetActive(true);
            }
        }
        else
        {
            WinTween.Kill();
            TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
            foreach (GameObject e in FireAnim_Objects)
            {
                e.SetActive(false);
            }
        }
    }

    private void CheckForFeaturesAnimation()
    {
        bool playJackpot = false;
        bool playScatter = false;
        bool playBonus = false;
        bool playFreespin = false;
        if (SocketManager.ResultData.jackpot.amount > 0)
        {
            playJackpot = true;
        }
        if (SocketManager.ResultData.scatter.amount > 0)
        {
            playScatter = true;
        }
        //if (SocketManager.ResultData.bonus.amount > 0)
        //{
        //    playBonus = true;
        //}
        if (SocketManager.ResultData.freeSpin.isFreeSpin)
        {
            playFreespin = true;
        }
        PlayFeatureAnimation(playJackpot, playScatter, playBonus, playFreespin);
    }
    private void PlayFeatureAnimation(bool jackpot = false, bool scatter = false, bool bonus = false, bool freeSpin = false)
    {
        for (int i = 0; i < SocketManager.ResultData.matrix.Count; i++)
        {
            for (int j = 0; j < SocketManager.ResultData.matrix[i].Count; j++)
            {

                if (int.TryParse(SocketManager.ResultData.matrix[i][j], out int parsedNumber))
                {
                    if (jackpot && parsedNumber == 12)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    }
                    if (scatter && parsedNumber == 11)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    }
                    if (bonus && parsedNumber == 18)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    }
                    if (freeSpin && parsedNumber == 13)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    }
                }

            }
        }
    }
    #endregion

    internal void CallCloseSocket()
    {
        StartCoroutine(SocketManager.CloseSocket());
    }


    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (BetMinus_Button) BetMinus_Button.interactable = toggle;
        if (BetPlus_Button) BetPlus_Button.interactable = toggle;

    }

    //start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        temp.StartAnimation();
        TempList.Add(temp);
    }

    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            TempList[i].StopAnimation();
        }
        TempList.Clear();
        TempList.TrimExcess();
    }


    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }



    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool isStop)
    {
        alltweens[index].Pause();
        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 0.5f).SetEase(Ease.OutElastic);
        if (!isStop)
        {
            yield return new WaitForSeconds(0.5f);
            if (audioController) audioController.StopWLAaudio();
        }
        else
        {
            yield return null;
        }
    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

