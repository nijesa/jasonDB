using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PokemonHttpTest : MonoBehaviour
{
    [SerializeField] private int pokemonID = 9;
    [SerializeField] private string URL = "https://pokeapi.co/api/v2/pokemon";
    [SerializeField] private RawImage pokemonImage;
    [SerializeField] private TMP_Text pokemonName;
    [SerializeField] private TMP_Text pokemonTypes;
    [SerializeField] private TMP_InputField idInputField;

    void Start()
    {
        StartCoroutine(GetPokemon(pokemonID));
    }

    // Llamar desde botón
    public void FindFromInputField()
    {
        if (int.TryParse(idInputField.text, out int id))
        {
            StartCoroutine(GetPokemon(id));
        }
        else
        {
            Debug.LogWarning("Ingresa un número válido de la Pokédex");
        }
    }

    IEnumerator GetPokemon(int id)
    {
        UnityWebRequest www = UnityWebRequest.Get(URL + "/" + id);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Pokemon pokemon = JsonUtility.FromJson<Pokemon>(www.downloadHandler.text);

            // Nombre
            pokemonName.text = pokemon.name.ToUpper();

            // Tipos
            string typesText = "";
            foreach (PokemonType t in pokemon.types)
            {
                typesText += t.type.name + " ";
            }
            pokemonTypes.text = typesText.Trim();

            // Imagen
            StartCoroutine(GetTexture(pokemon.sprites.front_default));
        }
    }

    IEnumerator GetTexture(string imageURL)
    {
        UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageURL);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(uwr.error);
        }
        else
        {
            pokemonImage.texture = DownloadHandlerTexture.GetContent(uwr);
        }
    }
}
