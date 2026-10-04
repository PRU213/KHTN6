using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class QuestionController : MonoBehaviour
{
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] answerButtons; // 4 buttons
    [SerializeField] private TMP_Text[] answerTexts; // 4 texts corresponding to buttons
    [SerializeField] private GameObject[] hearts; // Tim1, Tim2, Tim3
    
    [SerializeField] private QuestionData[] testQuestions;

    private QuestionData currentQuestion;

    /// <summary>
    /// Gọi từ bên ngoài để truyền danh sách câu hỏi (ví dụ từ Google Sheet).
    /// Nếu gọi trước khi Start(), câu hỏi sẽ được dùng ngay.
    /// </summary>
    public void SetQuestions(QuestionData[] questions)
    {
        testQuestions = questions;
    }

    void Start()
    {
        // Chỉ dùng câu hỏi mặc định nếu chưa có nguồn nào truyền vào
        if (testQuestions == null || testQuestions.Length == 0)
            InitializeTestQuestions();

        GameSession.hp = 3;
        UpdateHeartsDisplay();
        LoadQuestion();

        // Gắn sự kiện cho các nút
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i; // Copy value for closure
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }
    }

    private void InitializeTestQuestions()
    {
        testQuestions = new QuestionData[]
        {
            new QuestionData { question = "Thành phần nào sau đây không có ở tế bào động vật?", optionA = "Màng sinh chất", optionB = "Thành tế bào", optionC = "Nhân", optionD = "Ti thể", correct = "B" },
            new QuestionData { question = "Bào quan nào được ví như 'nhà máy điện' của tế bào?", optionA = "Lục lạp", optionB = "Bộ máy Golgi", optionC = "Ti thể", optionD = "Ribosome", correct = "C" },
            new QuestionData { question = "Đơn phân cấu tạo nên phân tử ADN là gì?", optionA = "Axit amin", optionB = "Nucleotide", optionC = "Monosaccharide", optionD = "Axit béo", correct = "B" },
            new QuestionData { question = "Loại bào quan nào có chức năng quang hợp ở thực vật?", optionA = "Lục lạp", optionB = "Không bào", optionC = "Trung thể", optionD = "Lysosome", correct = "A" },
            new QuestionData { question = "Trong quá trình phân bào, thoi phân bào được hình thành từ?", optionA = "Nhân con", optionB = "Trung thể", optionC = "Màng nhân", optionD = "Lưới nội chất", correct = "B" }
        };
    }

    private void LoadQuestion()
    {
        int index = GameSession.GetNextQuestionIndex() % testQuestions.Length;
        currentQuestion = testQuestions[index];

        questionText.text = currentQuestion.question;
        answerTexts[0].text = currentQuestion.optionA;
        answerTexts[1].text = currentQuestion.optionB;
        answerTexts[2].text = currentQuestion.optionC;
        answerTexts[3].text = currentQuestion.optionD;
    }

    private void OnAnswerClicked(int buttonIndex)
    {
        string[] options = { "a", "b", "c", "d" };
        string selectedOption = options[buttonIndex];

        if (selectedOption.Equals(currentQuestion.correct, System.StringComparison.OrdinalIgnoreCase))
        {
            // Trả lời đúng
            GameSession.OnAnswerCorrect();
            SceneManager.LoadScene(GameSession.gameplaySceneName);
        }
        else
        {
            // Trả lời sai
            GameSession.OnAnswerWrong();
            UpdateHeartsDisplay();

            if (GameSession.hp > 0)
            {
                LoadQuestion(); // Load câu hỏi tiếp theo
            }
            else
            {
                StartCoroutine(GameOverRoutine());
            }
        }
    }

    private void UpdateHeartsDisplay()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < GameSession.hp);
        }
    }

    private IEnumerator GameOverRoutine()
    {
        // Chờ 0.5s để người chơi thấy mất tim cuối
        yield return new WaitForSeconds(0.5f);
        GameSession.ResetAll();
        SceneManager.LoadScene(GameSession.gameplaySceneName);
    }
}
