using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class XepHangManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Dropdown chuongDropdown;
    
    [Header("Settings")]
    public string sheetCSVUrl = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";
    
    private List<string> chapters = new List<string>();
    private string selectedChapter = "Tất cả";

    void Start()
    {
        if (chuongDropdown == null)
        {
            chuongDropdown = GetComponentInChildren<Dropdown>();
        }

        if (chuongDropdown != null)
        {
            chuongDropdown.onValueChanged.AddListener(OnChapterSelected);
        }

        // Tự động tìm và gán chức năng cho nút QuayLai
        Transform quayLai = FindChildRecursive(transform, "QuayLai");
        if (quayLai != null)
        {
            Button btn = quayLai.GetComponent<Button>();
            if (btn == null) btn = quayLai.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("HomePage"));
        }

        StartCoroutine(LoadDataFromSheet());
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    IEnumerator LoadDataFromSheet()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(sheetCSVUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ParseCSV(req.downloadHandler.text);
                PopulateDropdown();
            }
            else
            {
                Debug.LogError("Failed to load questions: " + req.error);
            }
        }
    }

    private void ParseCSV(string csvData)
    {
        chapters.Clear();
        List<string[]> parsedRows = ParseCSVFully(csvData);
        bool isFirstRow = true;

        foreach (string[] cols in parsedRows)
        {
            if (isFirstRow)
            {
                isFirstRow = false;
                continue; 
            }

            if (cols.Length >= 12)
            {
                string monID = cols[1];
                string topic = cols[2];

                // Lọc Sinh học
                if (monID.Trim().Equals("Sinh học", StringComparison.OrdinalIgnoreCase))
                {
                    string chapterName = topic.Trim();
                    if (!string.IsNullOrEmpty(chapterName) && !chapters.Contains(chapterName))
                    {
                        chapters.Add(chapterName);
                    }
                }
            }
        }
    }

    private List<string[]> ParseCSVFully(string csvData)
    {
        List<string[]> rows = new List<string[]>();
        List<string> currentCols = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < csvData.Length; i++)
        {
            char c = csvData[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < csvData.Length && csvData[i + 1] == '\"')
                {
                    currentField += '\"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentCols.Add(currentField);
                currentField = "";
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvData.Length && csvData[i + 1] == '\n') i++;
                
                if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
                {
                    currentCols.Add(currentField);
                    rows.Add(currentCols.ToArray());
                }
                
                currentCols.Clear();
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
        {
            currentCols.Add(currentField);
            rows.Add(currentCols.ToArray());
        }

        return rows;
    }

    private void PopulateDropdown()
    {
        if (chuongDropdown == null) return;

        chuongDropdown.ClearOptions();

        List<string> options = new List<string>();
        options.Add("Tất cả"); // Default option

        foreach (string chapter in chapters)
        {
            options.Add(chapter);
        }

        chuongDropdown.AddOptions(options);
        chuongDropdown.value = 0; // Chọn "Tất cả" mặc định
    }

    private void OnChapterSelected(int index)
    {
        if (chuongDropdown != null && index >= 0 && index < chuongDropdown.options.Count)
        {
            selectedChapter = chuongDropdown.options[index].text;
            Debug.Log("Chương đã chọn trong xếp hạng: " + selectedChapter);
            
            // TODO: Gọi hàm filter danh sách xếp hạng dựa trên selectedChapter ở đây
            // FindObjectOfType<RankingDisplay>()?.FilterByChapter(selectedChapter);
        }
    }
}
