using System.CommandLine;

var rootCommand = new RootCommand();

var allOption = new Option("--all");
allOption.AddAlias("-a");
allOption.Description = "Show tasks in all lists";
rootCommand.AddOption(allOption);
var showCertainListOption = new Option("--list");
showCertainListOption.AddAlias("-l");
showCertainListOption.Description = "Show tasks of only this list";
rootCommand.AddOption(showCertainListOption);
var showListVerb = new Command("lists");
rootCommand.AddCommand(showListVerb);
showListVerb.Description = "Show all user lists";

var addVerb = new Command("add");
var listOption = new Option("--list");
listOption.AddAlias("-l");
listOption.Description = "Add a task to a certain list";
rootCommand.AddOption(listOption);
rootCommand.AddCommand(addVerb);
var addToday = new Option("--in-my-day");
addVerb.AddOption(addToday);
var addNoteOption = new Option("--note");
addNoteOption.AddAlias("-n");
addVerb.AddOption(addNoteOption);

var completeVerb = new Command("complete");
var completeIndexOption = new Command("--index");
completeIndexOption.AddAlias("-i");
rootCommand.AddCommand(completeVerb);