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

    public Text BetsText;

    public Text PointText;

    //パラメータ
    public int StartPoint = 20;

    int currentPoint;

    int currentBets;

    [Min(100)]
    public int ShuffleCount = 100;

    List<Card.Data> cards;

    public enum Action
    {
        WaitAction = 0,
        Hit = 1,
        Stand = 2
    }

    Action CurrentAction = Action.WaitAction;

    public void SetAction(int action)
    {
        CurrentAction = (Action) action;
    }

    private void Awake()
    {
        BetsInput.onValidateInput = BetsInputOnValidateInput;
        BetsInput.onValueChanged.AddListener (BetsInputOnValueChanged);
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

        while (true)
        {
            InitCards(); //カードを初期化する

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
            BetsInputDialog.SetActive(false);
            BetsText.text = currentBets.ToString();

            //カードを配る
            DealCards();

            // プレイヤーが行動を決めるまで待つ
            bool waitAction = true;
            do
            {
                CurrentAction = Action.WaitAction;
                yield return new WaitWhile(() =>
                            CurrentAction == Action.WaitAction);

                // 行う行動に合わせて処理を分岐する
                switch (CurrentAction)
                {
                    case Action.Hit:
                        Debug.Log("Hit");
                        PlayerDealCard();
                        waitAction = true;
                        break;
                    case Action.Stand:
                        Debug.Log("Stand");
                        waitAction = false;
                        break;
                    default:
                        waitAction = true;
                        throw new System.Exception("知らない行動をしようとしています。");
                }
            }
            while (waitAction);
            //行う行動に合わせて処理を分岐する
            //ゲームの結果を判定する
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
}