namespace Model.Contract;

public class Name(string firstName, string lastName)
{
    public string FirstName  { get; set; } = firstName;
    public string LastName  { get; set; } = lastName;
}  