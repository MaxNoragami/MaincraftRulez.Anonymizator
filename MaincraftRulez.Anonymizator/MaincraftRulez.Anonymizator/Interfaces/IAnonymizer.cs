namespace MaincraftRulez.Anonymizator;

public interface IAnonymizer
{
    string Anonymize(string input);
    string Deanonymize(string input);
    void SetPreserveCharacters(char[] characters);
    void SetPreservePattern(string pattern);
}