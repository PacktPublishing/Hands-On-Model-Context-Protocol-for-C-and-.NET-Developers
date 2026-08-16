using TravelBooking.Chapter14;

Console.WriteLine("Chapter 14 -- Deployment and operational resilience patterns");
Console.WriteLine(new string('=', 78));
Console.WriteLine("This runnable entry point provides orientation for the chapter snippets.");
Console.WriteLine();
Console.WriteLine("Included reference snippets:");
foreach (var section in Demos.CoveredSections)
{
    Console.WriteLine($"  - {section}.cs");
}
