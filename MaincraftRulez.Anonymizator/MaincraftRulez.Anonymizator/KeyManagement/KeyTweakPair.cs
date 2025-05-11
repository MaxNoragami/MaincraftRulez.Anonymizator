namespace MaincraftRulez.Anonymizator;

public class KeyTweakPair
{
    public string Key { get; private set; }
    public byte[] Tweak { get; private set; }
    public string Name { get; private set; }
    
    public KeyTweakPair(string key, byte[] tweak, string name = "")
    {
        Key = key;
        Tweak = tweak;
        Name = name;
    }
}