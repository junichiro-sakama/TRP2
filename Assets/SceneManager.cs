using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneManager : MonoBehaviour
{
    public Card CardPrefab;

    public GameObject Dealer;

    public GameObject Player;

    public GameObject BetsInputDialog;

    public InputField BetsInput;

    public Button BetsInputOKButton;

    public Button DoubleButton;

    public Text BetsText;

    public Text PointText;

    public Text ResultText;

    public float WaitResultSeconds = 2;

    public Text GoalPointText;

    public int goalPoint = 40;

    //パラメータ
    public int StartPoint = 20;

    public GameObject panels;

    int currentPoint;

    int currentBets;

    [Min(100)]
    public int ShuffleCount = 100;

    public int cardCount = 13 * 4;

    List<Card.Data> cards;

    public enum Action
    {
        WaitAction = 0,
        Hit = 1,
        Stand = 2,
        Double = 3,
        Surrender = 4
    }

    public enum Judge
    {
        NoState = 0,
        Win = 1,
        Lose = 2,
        Draw = 3
    }

    public enum Scenes
    {
        Title = 0,
        Main1 = 1,
        Main2 = 2,
        Ad = 3,
        GameOver = 4,
        Score = 5,
        Menu = 6
    }

    private bool screenTouched = false;

    private bool isCardShuffle = false;

    Action CurrentAction = Action.WaitAction;

    public void SetAction(int action)
    {
        CurrentAction = (Action) action;
    }

    private void Awake()
    {
        BetsInput.onValidateInput = BetsInputOnValidateInput;
        BetsInput.onValueChanged.AddListener (BetsInputOnValueChanged);
        GoalPointText.text = goalPoint.ToString();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) screenTouched = true;
    }

    char BetsInputOnValidateInput(string text, int startIndex, char addedChar)
    {
        if (!char.IsDigit(addedChar)) return '\0';
        return addedChar;
    }

    void BetsInputOnValueChanged(string text)
    {
        BetsInputOKButton.interactable = false;
        if (int.TryParse(BetsInput.text, out var bets))
        {
            if (0 < bets && bets <= currentPoint)
            {
                BetsInputOKButton.interactable = true;
            }
        }
    }

    IEnumerator GameLoop()
    {
        currentPoint = StartPoint;
        BetsText.text = "0";
        PointText.text = currentPoint.ToString();

        ResultText.gameObject.SetActive(false);

        InitCards(); //カードを初期化する

        while (true)
        {
            if (isCardShuffle)
            {
                InitCards(); //カードを初期化する

                // カードをシャッフルしたことを表示
                ResultText.text = "Card Shuffle!!";
                ResultText.gameObject.SetActive(true);
                screenTouched = false;
                while (!screenTouched)
                {
                    yield return new WaitForSeconds((float) 0.1);
                }
                if (!isCardShuffle) UpdateDisplyScene(Scenes.Main1);
                screenTouched = false;
                ResultText.gameObject.SetActive(false);
                UpdateDisplyScene(Scenes.Main1);
                isCardShuffle = false;
            }

            yield return null; //何か実装するまで残しておく

            //ベットを決めるまで待つ
            do
            {
                BetsInputDialog.SetActive(true);
                yield return new WaitWhile(() => BetsInputDialog.activeSelf);

                //入力したテキストを使用できるものかチェックする
                if (int.TryParse(BetsInput.text, out var bets))
                {
                    if (0 < bets && bets <= currentPoint)
                    {
                        currentBets = bets;
                        break;
                    }
                }
            }
            while (true);

            //画面の更新
            // BetsInputDialog.SetActive(false);
            BetsText.text = currentBets.ToString();

            //Doubleが選択できるかの判定
            if (currentPoint < currentBets * 2)
            {
                DoubleButton.interactable = false;
            }
            else
            {
                DoubleButton.interactable = true;
            }

            //カードを配る
            DealCards();

            //初期配置でエースを11に設定
            {
                var dealedCards = new List<Card>();
                foreach (var
                    card
                    in
                    Player.transform.GetComponentsInChildren<Card>()
                )
                dealedCards.Add(card);

                dealedCards.Reverse();
                for (var i = 1; 0 <= i; i--)
                {
                    if (dealedCards[i].Number == 1)
                    {
                        dealedCards[i].SetAceAsEleven();
                        break;
                    }
                }
            }

            // プレイヤーが行動を決めるまで待つ
            bool waitAction = true;
            Judge doWin = Judge.NoState;
            bool firstChoice = true;
            do
            {
                CurrentAction = Action.WaitAction;

                // 最初の選択のみDouble可能
                if (!firstChoice) DoubleButton.interactable = false;

                yield return new WaitWhile(() =>
                            CurrentAction == Action.WaitAction);

                // 行う行動に合わせて処理を分岐する
                switch (CurrentAction)
                {
                    case Action.Hit:
                        PlayerDealCard();
                        waitAction = true;
                        if (!CheckPlayerCard())
                        {
                            waitAction = false;
                            doWin = Judge.Lose;
                        }

                        // カードをシャッフルするかの判定
                        isCardShuffle = cards.Count < cardCount * 0.65;
                        break;
                    case Action.Stand:
                        // カードをシャッフルするかの判定
                        isCardShuffle = cards.Count < cardCount * 0.65;
                        waitAction = false;
                        doWin = StandAction();
                        break;
                    case Action.Double:
                        PlayerDealCard();
                        waitAction = false;

                        // 掛け金を2倍にする
                        currentBets *= 2;

                        //画面の更新
                        BetsText.text = currentBets.ToString();

                        // カードをシャッフルするかの判定
                        isCardShuffle = cards.Count < cardCount * 0.65;
                        if (!CheckPlayerCard())
                        {
                            waitAction = false;
                            doWin = Judge.Lose;
                            break;
                        }
                        doWin = StandAction();
                        break;
                    case Action.Surrender:
                        waitAction = false;
                        doWin = Judge.Lose;

                        // 掛け金の半分だけ取られる処理を書く
                        currentBets /= 2;

                        // カードをシャッフルするかの判定
                        isCardShuffle = cards.Count < cardCount * 0.65;
                        break;
                    default:
                        waitAction = true;
                        throw new System.Exception("知らない行動をしようとしています。");
                }
                firstChoice = false;
            }
            while (waitAction);

            //行う行動に合わせて処理を分岐する
            //ゲームの結果を判定する
            ResultText.gameObject.SetActive(true);
            switch (doWin)
            {
                case Judge.NoState:
                    break;
                case Judge.Win:
                    currentPoint += currentBets;
                    ResultText.text = "Win!! + " + currentBets;
                    break;
                case Judge.Lose:
                    currentPoint -= currentBets;
                    ResultText.text = "Lose... - " + currentBets;
                    break;
                case Judge.Draw:
                    ResultText.text = "Draw";
                    break;
                default:
                    break;
            }

            screenTouched = false;
            while (!screenTouched)
            {
                yield return new WaitForSeconds((float) 0.1);
            }
            if (!isCardShuffle) UpdateDisplyScene(Scenes.Main1);
            screenTouched = false;
            PointText.text = currentPoint.ToString();
            ResultText.gameObject.SetActive(false);

            //ゲームオーバー・ゲームクリア処理
            if (currentPoint <= 0)
            {
                UpdateDisplyScene(Scenes.Ad);
                break;
            }
            if (currentPoint >= goalPoint)
            {
                ResultText.gameObject.SetActive(true);
                ResultText.text = "Game Clear!!";
                break;
            }
        }
    }

    Coroutine _gameLoopCoroutine;

    private void Start()
    {
        _gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    void InitCards()
    {
        cards = new List<Card.Data>(13 * 4);
        var marks =
            new List<Card.Mark>()
            {
                Card.Mark.Heart,
                Card.Mark.Diamond,
                Card.Mark.Spade,
                Card.Mark.Crub
            };

        foreach (var mark in marks)
        {
            for (var num = 1; num <= 13; ++num)
            {
                var card = new Card.Data() { Mark = mark, Number = num };
                cards.Add (card);
            }
        }

        ShuffleCards();
    }

    void ShuffleCards()
    {
        //シャッフルする
        var random = new System.Random();
        for (var i = 0; i < ShuffleCount; ++i)
        {
            var index = random.Next(cards.Count);
            var index2 = random.Next(cards.Count);

            //カードの位置を入れ替える。
            var tmp = cards[index];
            cards[index] = cards[index2];
            cards[index2] = tmp;
        }
    }

    Card.Data DealCard()
    {
        if (cards.Count <= 0) return null;

        var card = cards[0];
        cards.Remove (card);
        return card;
    }

    void DealCards()
    {
        foreach (Transform card in Dealer.transform)
        {
            Object.Destroy(card.gameObject);
        }

        foreach (Transform card in Player.transform)
        {
            Object.Destroy(card.gameObject);
        }

        {
            //ディーラーに２枚カードを配る
            var holeCardObj = Object.Instantiate(CardPrefab, Dealer.transform);
            var holeCard = DealCard();
            holeCardObj.SetCard(holeCard.Number, holeCard.Mark, true);

            var upCardObj = Object.Instantiate(CardPrefab, Dealer.transform);
            var upCard = DealCard();
            upCardObj.SetCard(upCard.Number, upCard.Mark, false);
        }

        {
            //プレイヤーにカードを２枚配る
            for (var i = 0; i < 2; ++i)
            {
                var cardObj = Object.Instantiate(CardPrefab, Player.transform);
                var card = DealCard();

                cardObj.SetCard(card.Number, card.Mark, false);
            }
        }
    }

    void PlayerDealCard()
    {
        var cardObj = Object.Instantiate(CardPrefab, Player.transform);
        var card = DealCard();
        cardObj.SetCard(card.Number, card.Mark, false);
    }

    bool CheckPlayerCard()
    {
        var sumNumber = 0;
        sumNumber = CheckCard(Player);
        return (sumNumber <= 21);
    }

    bool CheckDealerCard()
    {
        var sumNumber = 0;
        sumNumber = CheckCard(Dealer);
        return (sumNumber <= 21);
    }

    void OpenCard()
    {
        foreach (var card in Dealer.transform.GetComponentsInChildren<Card>())
        {
            if (card.IsReverse)
            {
                //裏面のカードを表向きにする
                card.SetCard(card.Number, card.CurrentMark, false);
            }
        }
    }

    Judge StandAction()
    {
        var sumPlayerNumber = 0;
        sumPlayerNumber = CheckCard(Player);

        int STAND_NUMER = 17;
        var sumDealerNumber = 0;
        sumDealerNumber = CheckCard(Dealer);

        OpenCard();

        // ディーラーがカードを引く条件
        while (sumDealerNumber < sumPlayerNumber &&
            sumDealerNumber < STAND_NUMER
        )
        {
            var cardObj = Object.Instantiate(CardPrefab, Dealer.transform);
            var card = DealCard();
            cardObj.SetCard(card.Number, card.Mark, false);
            sumDealerNumber += card.Number;
        }

        if (!CheckDealerCard())
        {
            return Judge.Win;
        }

        if (sumDealerNumber < sumPlayerNumber)
        {
            return Judge.Win;
        }
        else if (sumDealerNumber > sumPlayerNumber)
        {
            return Judge.Lose;
        }
        else
        {
            return Judge.Draw;
        }
    }

    int CheckCard(GameObject gameObject)
    {
        var sumNumber = 0;
        var aceCount = 0;

        // エースのオブジェクトを記録しておくリスト
        var aceCards = new List<Card>();

        foreach (var
            card
            in
            gameObject.transform.GetComponentsInChildren<Card>()
        )
        {
            if (card.UseNumber == 1)
            {
                aceCount += 1;
                aceCards.Add (card);
            }
            sumNumber += card.UseNumber;
        }

        if (aceCount != 0 && sumNumber <= 21)
        {
            for (var i = 0; i < aceCount; i++)
            {
                if (sumNumber + 10 <= 21)
                {
                    sumNumber += 10;
                    aceCards[i].SetAceAsEleven();
                }
                else
                {
                    aceCards[i].SetAceAsOne();
                }
            }
        }
        return sumNumber;
    }

    void UpdateDisplyScene(Scenes scene)
    {
        switch (scene)
        {
            case Scenes.Title:
                panels.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                break;
            case Scenes.Main1:
                panels.transform.localPosition =
                    new Vector3(-600.0f, 0.0f, 0.0f);
                break;
            case Scenes.Main2:
                panels.transform.localPosition =
                    new Vector3(-1200.0f, 0.0f, 0.0f);
                break;
            case Scenes.Ad:
                panels.transform.localPosition =
                    new Vector3(-1800.0f, 0.0f, 0.0f);
                break;
            case Scenes.GameOver:
                panels.transform.localPosition =
                    new Vector3(-2400.0f, 0.0f, 0.0f);
                break;
            case Scenes.Score:
                panels.transform.localPosition =
                    new Vector3(-3000.0f, 0.0f, 0.0f);
                break;
            case Scenes.Menu:
                panels.transform.localPosition =
                    new Vector3(-3600.0f, 0.0f, 0.0f);
                break;
        }
    }

    public void PushTitleButton()
    {
        UpdateDisplyScene(Scenes.Title);
    }

    public void PushStartButton()
    {
        UpdateDisplyScene(Scenes.Main1);
    }

    public void PushDealButton()
    {
        UpdateDisplyScene(Scenes.Main2);
    }

    public void PushAdSkipButton()
    {
        UpdateDisplyScene(Scenes.GameOver);
    }

    public void PushRetryButton()
    {
        UpdateDisplyScene(Scenes.Main1);
    }

    public void PushScoreButton()
    {
        UpdateDisplyScene(Scenes.Score);
    }

    public void PushMenuButton()
    {
        UpdateDisplyScene(Scenes.Menu);
    }
}
