using System;

[Serializable]
public class Pokemon
{
    public int id;
    public string name;
    public PokemonType[] types;
    public PokemonSprites sprites;
}

[Serializable]
public class PokemonType
{
    public PokemonTypeInfo type;
}

[Serializable]
public class PokemonTypeInfo
{
    public string name;
}

[Serializable]
public class PokemonSprites
{
    public string front_default;
}

[Serializable]
public class PokemonIDs
{
    public string id;
}