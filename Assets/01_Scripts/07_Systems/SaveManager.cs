using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class SaveManager
{
    // 파일 이름만 상수로 관리 (오타 방지)
    private const string GameSaveFileName = "save.json";
    private const string MetaSaveFileName = "meta.json"; // 엔딩수집

    // 경로를 가져오는 전용 함수
    private string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);
    //Application.persistentDataPath: 유니티가 제공하는 OS별 공식 저장 폴더 주소
    //Path.Combine: 주소와 파일 이름을 합쳐줌. ex) C:/MyGame/save.json

    //  게임 데이터 저장/로드
    public void SaveGame(GameSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true); // data를 json으로 변환, true를 넣으면 자동 줄바꿈.
            File.WriteAllText(GetPath(GameSaveFileName), json); // 지점 된 경로에 텍스트 파일을 새로 만들고 이미 있을경우 덮어씀.
            Debug.Log($"게임 데이터 저장 완료: {GetPath(GameSaveFileName)}"); // data = 데이터, json변환 = 포장, save.json(파일) = 도시락통, File.WriteAllText = 통에 담는 행위
        }

        catch(System.Exception e)
        {
            Debug.LogError($"저장실패 {e.Message}");
        }
    }

    public GameSaveData LoadGame()
    {
        string path = GetPath(GameSaveFileName);
        if (!File.Exists(path)) return null; // 저장된 파일 없으면 null 반환

        try
        {
            string json = File.ReadAllText(path); // json에 텍스트 내용을 담음
            return JsonUtility.FromJson<GameSaveData>(json); // 역직렬화
        }
        catch (System.Exception)
        {
            return null;
        }

    }

    // 엔딩 메타 정보 저장/로드 (루프 리셋 시에도 유지됨)
    public void SaveMeta(EndingData meta) // 엔딩 저장.
    {
        try
        {
            string json = JsonUtility.ToJson(meta, true);
            File.WriteAllText(GetPath(MetaSaveFileName), json);
            Debug.Log($"[Meta] 엔딩 정보 저장 완료: {GetPath(MetaSaveFileName)}");
        }
        catch(System.Exception e)
        {
            Debug.LogError($"엔딩저장실패 {e.Message}");
        }
    }

    public EndingData LoadMeta()
    {
        string path = GetPath(MetaSaveFileName);
        if (!File.Exists(path)) return new EndingData(); // 메타 정보는 없으면 새로 만듦, null반환이 아닌 엔딩 수집 0개로 표시.

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<EndingData>(json);
    }
}

