using System;
using System.Collections.Generic;

namespace Design_Develop_Deploy_Project.Utilities
{
    public static class ConsoleHelper
    {
        public static void PrintList(List<string> items, string header = null, bool showNumbers = true)
        {
            Console.Clear();

            if (!string.IsNullOrWhiteSpace(header))
            {
                Console.WriteLine(header);
                Console.WriteLine(new string('=', header.Length));
            }

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("No items to display.");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (showNumbers)
                    Console.WriteLine($"{i + 1}. {items[i]}");
                else
                    Console.WriteLine(items[i]);
            }
        }

        /// <summary>
        /// Prints a section of text with a header and optional divider line.
        /// </summary>
        public static void PrintSection(string header, string content, string colour = "White")
        {

            if (Enum.TryParse(colour, true, out ConsoleColor colourValue))
            {
                Console.ForegroundColor = colourValue;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine($"\n{header}");
            Console.WriteLine(new string('-', header.Length));
            Console.WriteLine(content);
            Console.ResetColor();
        }

        /// <summary>
        /// Prints a simple message and waits for user input.
        /// </summary>
        public static void Pause(string message = "Press any key to continue...")
        {
            Console.WriteLine();
            Console.WriteLine(message);
            Console.ReadKey();
        }

        public static int PromptForChoice(List<string> items, string header = null)
        {
            while (true)
            {
                PrintList(items, header);

                Console.Write("\nSelect an option (1 - {0}): ", items.Count);
                string input = Console.ReadLine()?.Trim();

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= items.Count)
                    return choice;

                Console.WriteLine("\nInvalid selection. Please enter a number between 1 and {0}.", items.Count);
                Pause("Press any key to try again...");
                Console.Clear();
            }
        }

        public static bool GetYesOrNo(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n): ");
                string input = Console.ReadLine()?.Trim().ToLower();

                if (input == "y" || input == "yes")
                    return true;
                if (input == "n" || input == "no")
                    return false;

                Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
            }
        }

        public static string AskForInput(string prompt)
        {
            Console.Write($"{prompt}: ");
            string input = Console.ReadLine()?.Trim();
            return input ?? string.Empty;
        }

        public static void WriteInColour(string prompt, string colour) 
        {
            if (Enum.TryParse(colour, true, out ConsoleColor colourValue))
            {
                Console.ForegroundColor = colourValue;
            }
            else 
            {
                Console.ForegroundColor= ConsoleColor.White;
            }

            Console.WriteLine(prompt);
            Console.ResetColor();
        }
    }
}
