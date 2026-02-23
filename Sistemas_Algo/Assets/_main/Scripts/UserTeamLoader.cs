using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserTeamLoader : MonoBehaviour
{
    [SerializeField] private string usersURL =
        "https://my-json-server.typicode.com/nijesa/jasonDB/users";
    [SerializeField] private string pokemonURL =
        "https://pokeapi.co/api/v2/pokemon";

    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private RawImage[] pokemonImages;
    [SerializeField] private TMP_Text[] pokemonNames;
    [SerializeField] private TMP_Text[] pokemonTypes;
    [SerializeField] private TMP_Text[] pokemonID_PH;

     void Start()
    {
        LoadUserByID(1);
    }

    public void LoadUserByID(int userID)
    {
        StopAllCoroutines();
        StartCoroutine(GetUser(userID));
    }

    IEnumerator GetUser(int id)
    {
        UnityWebRequest www = UnityWebRequest.Get(usersURL + "/" + id);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
        }

        User user = JsonUtility.FromJson<User>(www.downloadHandler.text);
        userNameText.text = user.username;
        Debug.Log(userNameText.text+"1entró");
        Debug.Log(user.username+"2entró");

        for (int i = 0; i < user.deck.Length && i < 6; i++)
        {
            StartCoroutine(GetPokemon(user.deck[i], i));
        }
        userNameText.text = user.username;
    }

    IEnumerator GetPokemon(int pokemonID, int slot)
    {
        UnityWebRequest www = UnityWebRequest.Get(pokemonURL + "/" + pokemonID);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
        }

        Pokemon pokemon = JsonUtility.FromJson<Pokemon>(www.downloadHandler.text);

        pokemonNames[slot].text = pokemon.name.ToUpper();

        string types = "";
        foreach (PokemonType t in pokemon.types)
        {
            types += t.type.name + " / ";
        }
        pokemonTypes[slot].text = types.Trim();

        
        pokemonID_PH[slot].text = "Numero de la pokedex: " + pokemon.id.ToString();

        StartCoroutine(GetTexture(pokemon.sprites.front_default, slot));
    }

    IEnumerator GetTexture(string url, int slot)
    {
        UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.Success)
        {
            pokemonImages[slot].texture =
                DownloadHandlerTexture.GetContent(uwr);
        }
    }
}


[Serializable]
public class User
{
    public int id;
    public string username;
    public int[] deck;
}