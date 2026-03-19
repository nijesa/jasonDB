using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TMPro;
using System;

public class AuthHandler : MonoBehaviour
{
    [Header("API")]
    [Tooltip("URL base del backend (editar si es necesario)")]
    [SerializeField] private string apiUrl = "https://sid-restapi.onrender.com";

    [Header("Panels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;
    [SerializeField] private GameObject panelUser;
    [SerializeField] private GameObject panelLeaderboard;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private TMP_Text loginStatusText;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_Text registerStatusText;

    [Header("User UI")]
    [SerializeField] private TMP_Text usernameLabel;
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private GameObject logoutButton;

    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardContent; // contenedor donde instanciar filas
    [SerializeField] private GameObject leaderboardRowPrefab; // prefab con 2 TMP_Text: username & score
    [SerializeField] private TMP_Text leaderboardStatusText;

    [Header("Endpoints (si necesitas personalizar)")]
    [SerializeField] private string loginEndpoint = "/api/auth/login";
    [SerializeField] private string registerEndpoint = "/api/usuarios";
    [SerializeField] private string profileEndpoint = "/api/usuarios/"; // + username
    [SerializeField] private string scoresEndpoint = "/api/scores"; // POST to update, GET to list

    // runtime
    private string token;
    private string username;

    private void Start()
    {
        // Muestra el nombre del proyecto (editar desde Inspector)
        if (projectNameText != null && string.IsNullOrEmpty(projectNameText.text))
            projectNameText.text = "Proyecto: [Tu Nombre Completo]";

        // Cargar token/username guardados
        token = PlayerPrefs.GetString("Token", null);
        username = PlayerPrefs.GetString("Username", null);

        // UI inicial
        ShowOnlyPanel(panelLogin);

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            // Si hay token, validarlo con el servidor
            StartCoroutine(GetProfile());
        }
    }

    #region UI helpers
    private void ShowOnlyPanel(GameObject toShow)
    {
        if (panelLogin) panelLogin.SetActive(panelLogin == toShow);
        if (panelRegister) panelRegister.SetActive(panelRegister == toShow);
        if (panelUser) panelUser.SetActive(panelUser == toShow);
        if (panelLeaderboard) panelLeaderboard.SetActive(panelLeaderboard == toShow);
    }

    private void SetStatus(TMP_Text statusField, string message)
    {
        if (statusField != null) statusField.text = message;
    }
    #endregion

    #region Registration
    // Llamar desde botón "Go to Register"
    public void ShowRegisterPanel()
    {
        ShowOnlyPanel(panelRegister);
        SetStatus(registerStatusText, "");
    }

    // Llamar desde botón "Register"
    public void RegisterButtonHandler()
    {
        string u = registerUsernameInput?.text?.Trim();
        string p = registerPasswordInput?.text;
        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            SetStatus(registerStatusText, "Rellena username y password.");
            return;
        }
        StartCoroutine(RegisterCoroutine(u, p));
    }

    IEnumerator RegisterCoroutine(string usernameToRegister, string passwordToRegister)
{
    RegisterData data = new RegisterData { username = usernameToRegister, password = passwordToRegister };
    string json = JsonUtility.ToJson(data);

    string url = apiUrl + registerEndpoint;

    UnityWebRequest www = new UnityWebRequest(url, "POST");
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
    www.downloadHandler = new DownloadHandlerBuffer();
    www.SetRequestHeader("Content-Type", "application/json");

    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Register failed: " + www.error);
        SetStatus(registerStatusText, "Error en registro");
    }
    else
    {
        Debug.Log("Register success: " + www.downloadHandler.text);

        // Parseamos la respuesta
        RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(www.downloadHandler.text);

        if (response != null && response.usuario != null)
        {
            SetStatus(registerStatusText, "Usuario creado. Ahora inicia sesión.");

            // Volver al login automáticamente
            yield return new WaitForSeconds(1f);
            ShowOnlyPanel(panelLogin);
        }
        else
        {
            SetStatus(registerStatusText, "Respuesta inesperada");
        }
    }
}
    #endregion

    #region Login + Profile validation
    // Conectar al botón Login
    public void LoginButtonHandler()
    {
        string u = loginUsernameInput?.text?.Trim();
        string p = loginPasswordInput?.text;
        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            SetStatus(loginStatusText, "Rellena username y password.");
            return;
        }
        StartCoroutine(LoginCoroutine(u, p));
    }

    private IEnumerator LoginCoroutine(string usernameToLogin, string passwordToLogin)
    {
        AuthData auth = new AuthData { username = usernameToLogin, password = passwordToLogin };
        string json = JsonUtility.ToJson(auth);

        string url = apiUrl.TrimEnd('/') + loginEndpoint;
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SetStatus(loginStatusText, "Login falló: " + www.error);
                Debug.LogError("Login failed: " + www.error + " - " + www.downloadHandler.text);
            }
            else
            {
                // Mapear respuesta a AuthResponse (asegúrate que el backend responda en ese formato)
                try
                {
                    AuthResponse resp = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

                    if (resp != null && !string.IsNullOrEmpty(resp.token))
                    {
                        token = resp.token;
                        username = resp.usuario != null ? resp.usuario.username : usernameToLogin;

                        PlayerPrefs.SetString("Token", token);
                        PlayerPrefs.SetString("Username", username);
                        PlayerPrefs.Save();

                        SetStatus(loginStatusText, "Login correcto.");
                        SetUIForUserLogged();
                    }
                    else
                    {
                        // Si no viene token, mostramos el texto crudo del servidor
                        SetStatus(loginStatusText, "Respuesta inesperada: " + www.downloadHandler.text);
                    }
                }
                catch (Exception e)
                {
                    SetStatus(loginStatusText, "Error al parsear respuesta.");
                    Debug.LogError("Parse error login: " + e + " raw: " + www.downloadHandler.text);
                }
            }
        }
    }

    // Verifica token pidiendo perfil (usado al inicio)
    public IEnumerator GetProfile()
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
        {
            ShowOnlyPanel(panelLogin);
            yield break;
        }

        string url = apiUrl.TrimEnd('/') + profileEndpoint + username;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("x-token", token);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Token inválido o error al obtener perfil: " + www.error);
                // Borrar token y forzar login
                LogoutLocal();
                ShowOnlyPanel(panelLogin);
            }
            else
            {
                // Si todo ok
                SetUIForUserLogged();
            }
        }
    }
    #endregion

    #region UI state for logged user
    private void SetUIForUserLogged()
    {
        // Mostrar panel de usuario
        ShowOnlyPanel(panelUser);

        if (usernameLabel != null)
            usernameLabel.text = "Welcome, " + username;

        if (logoutButton != null)
            logoutButton.SetActive(true);

        // Cargar leaderboard inmediatamente (opcional)
        StartCoroutine(GetLeaderboardCoroutine());
    }

    public void LogoutButtonHandler()
    {
        // Si quieres avisar al servidor, hazlo aquí (opcional)
        LogoutLocal();
        ShowOnlyPanel(panelLogin);
    }

    private void LogoutLocal()
    {
        token = null;
        username = null;
        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();
        if (usernameLabel != null) usernameLabel.text = "Welcome";
    }
    #endregion

    #region Scores / Leaderboard
    // Método público para actualizar el score del usuario (llamar desde tu juego)
    public void UpdateScoreFromGame(int newScore)
    {
        StartCoroutine(UpdateScoreCoroutine(newScore));
    }

    // También un handler para UI si quieres pasar un campo de texto
    public void UpdateScoreButtonHandler(TMP_InputField scoreInputField)
    {
        if (scoreInputField == null)
        {
            Debug.LogWarning("InputField de score no asignado.");
            return;
        }

        if (!int.TryParse(scoreInputField.text, out int parsed))
        {
            Debug.LogWarning("Score no válido.");
            if (leaderboardStatusText != null) leaderboardStatusText.text = "Score no válido.";
            return;
        }

        StartCoroutine(UpdateScoreCoroutine(parsed));
    }


    private IEnumerator UpdateScoreCoroutine(int score)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Usuario no autenticado. No se puede actualizar score.");
            yield break;
        }

        UpdateUserData data = new UpdateUserData();
        data.username = username;
        data.data = new ScoreData { score = score };

        string json = JsonUtility.ToJson(data);

        string url = apiUrl.TrimEnd('/') + "/api/usuarios";
        using (UnityWebRequest www = new UnityWebRequest(url, "PATCH"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("x-token", token);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error actualizando score: " + www.error + " - " + www.downloadHandler.text);
                if (leaderboardStatusText != null) leaderboardStatusText.text = "Error actualizando score.";
            }
            else
            {
                if (leaderboardStatusText != null) leaderboardStatusText.text = "Score actualizado.";
                // Refrescar tabla
                StartCoroutine(GetLeaderboardCoroutine());
            }
        }
    }
    public void GetLeaderboardButtonHandler()
{
    StartCoroutine(GetLeaderboardCoroutine());
}
    // Obtener todos los scores (suponemos que servidor responde con array JSON de ScoreEntry)
    IEnumerator GetLeaderboardCoroutine()
{
    // ✅ usamos tu apiUrl (no lo quitamos)
    string url = apiUrl + "/api/usuarios?limit=20&skip=0&sort=true";

    UnityWebRequest www = UnityWebRequest.Get(url);

    // ✅ TOKEN (igual que ya hacías en profile)
    www.SetRequestHeader("x-token", token);

    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Error al obtener leaderboard: " + www.error);

        if (leaderboardStatusText != null)
            leaderboardStatusText.text = "Error cargando leaderboard";
    }
    else
    {
        Debug.Log("Leaderboard: " + www.downloadHandler.text);

        LeaderboardResponse response =
            JsonUtility.FromJson<LeaderboardResponse>(www.downloadHandler.text);

        MostrarLeaderboard(response);
    }
}
void MostrarLeaderboard(LeaderboardResponse response)
{
    // limpiar contenido anterior
    foreach (Transform child in leaderboardContent)
    {
        Destroy(child.gameObject);
    }

    var sortedUsers = response.usuarios
        .OrderByDescending(user => user.data.score);

    foreach (UserScore user in sortedUsers)
    {
        GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContent);

        TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

        texts[0].text = user.username;
        texts[1].text = user.data.score.ToString();
    }

    if (leaderboardStatusText != null)
        leaderboardStatusText.text = "Leaderboard cargado";
}

    private void PopulateLeaderboard(List<ScoreEntry> entries)
    {
        // Limpiar contenedor
        foreach (Transform child in leaderboardContent) Destroy(child.gameObject);

        if (entries == null || entries.Count == 0)
        {
            SetStatus(leaderboardStatusText, "No hay puntajes aún.");
            return;
        }

        int rank = 1;
        foreach (var e in entries)
        {
            GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContent);
            // Esperamos que el prefab tenga dos TextMeshProUGUI con nombres "UsernameText" y "ScoreText"
            TMP_Text uname = row.transform.Find("UsernameText")?.GetComponent<TMP_Text>();
            TMP_Text stext = row.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            if (uname != null) uname.text = $"{rank}. {e.username}";
            if (stext != null) stext.text = e.score.ToString();

            rank++;
        }
    }
    #endregion
}

#region Data models

[Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[Serializable]
public class RegisterData
{
    public string username;
    public string password;
}

[Serializable]
public class usuario
{
    public string _id;
    public string username;
}

[Serializable]
public class AuthResponse
{
    public usuario usuario;
    public string token;
}

[Serializable]
public class ScoreData
{
    public int score;
}

[Serializable]
public class UpdateUserData
{
    public string username;
    public ScoreData data;
}

[Serializable]
public class ScoreEntry
{
    public string username;
    public int score;
}

// wrapper for cases where server devuelve { "scores": [ ... ] }
[Serializable]
public class LeaderboardWrapper
{
    public ScoreEntry[] scores;
}

#endregion

#region JsonHelper (para parsear arrays con JsonUtility)

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    public static T[] FromJson<T>(string json)
    {
        // Si el json ya tiene la forma {"array":[...]} usamos directamente
        if (json.StartsWith("{") && json.Contains("\"array\"")) 
        {
            Wrapper<T> w = JsonUtility.FromJson<Wrapper<T>>(json);
            return w.array;
        }

        // Si es un array puro como [ {...}, {...} ]
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T> { array = array };
        return JsonUtility.ToJson(wrapper);
    }
}
#endregion

[System.Serializable]
public class RegisterResponse
{
    public usuario usuario;
}



[System.Serializable]
public class UserScore
{
    public string username;
    public ScoreData data;
}

[System.Serializable]
public class LeaderboardResponse
{
    public UserScore[] usuarios;
}