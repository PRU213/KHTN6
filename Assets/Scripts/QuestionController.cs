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
            new QuestionData { question = "Thành phần nào sau đây không có ở tế bào động vật?", option_a = "Màng sinh chất", option_b = "Thành tế bào", option_c = "Nhân", option_d = "Ti thể", correct_option = "b" },
            new QuestionData { question = "Bào quan nào được ví như 'nhà máy điện' của tế bào?", option_a = "Lục lạp", option_b = "Bộ máy Golgi", option_c = "Ti thể", option_d = "Ribosome", correct_option = "c" },
            new QuestionData { question = "Đơn phân cấu tạo nên phân tử ADN là gì?", option_a = "Axit amin", option_b = "Nucleotide", option_c = "Monosaccharide", option_d = "Axit béo", correct_option = "b" },
            new QuestionData { question = "Loại bào quan nào có chức năng quang hợp ở thực vật?", option_a = "Lục lạp", option_b = "Không bào", option_c = "Trung thể", option_d = "Lysosome", correct_option = "a" },
            new QuestionData { question = "Trong quá trình phân bào, thoi phân bào được hình thành từ?", option_a = "Nhân con", option_b = "Trung thể", option_c = "Màng nhân", option_d = "Lưới nội chất", correct_option = "b" }
        };
    }

    private void LoadQuestion()
    {
        int index = GameSession.GetNextQuestionIndex() % testQuestions.Length;
        currentQuestion = testQuestions[index];

        questionText.text = currentQuestion.question;
        answerTexts[0].text = currentQuestion.option_a;
        answerTexts[1].text = currentQuestion.option_b;
        answerTexts[2].text = currentQuestion.option_c;
        answerTexts[3].text = currentQuestion.option_d;
    }

    private void OnAnswerClicked(int buttonIndex)
    {
        string[] options = { "a", "b", "c", "d" };
        string selectedOption = options[buttonIndex];

        if (selectedOption == currentQuestion.correct_option)
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
