using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace ConsoleApp2
{
    class Program
    {
        static string URL = "https://ponychallenge.trustpilot.com/pony-challenge/maze";
        static string MazeId = "";
        static HttpClient client = new HttpClient();        
        static List<String> listOfSteps = new List<string>();

        public class Maze
        {

            public int[] Pony { get; set; }
            public int[] Domokun { get; set; }
            [JsonProperty(PropertyName = "end-point")]
            public int[] EndPoint { get; set; }
            public int[] Size { get; set; }
            public string[][] Data { get; set; }

        }
     
        public class MazePostObject
        {
            [JsonProperty(PropertyName = "maze-width")]
            public int Width { get; set; }
            [JsonProperty(PropertyName = "maze-height")]
            public int Height { get; set; }
            [JsonProperty(PropertyName = "maze-player-name")]
            public string PlayerName { get; set; }
            [JsonProperty(PropertyName = "difficulty")]
            public int Difficulty { get; set; }            
        }

        public class MazeResponseObject
        {
            [JsonProperty(PropertyName = "maze_id")]
            public string Id { get; set; }
        }

        public class MoveResponseObject
        {
            [JsonProperty(PropertyName = "state")]
            public string State { get; set; }
        }

        public class PathNode
        {
            public PathNode North { get; set; } = null;
            public PathNode East { get; set; } = null;
            public PathNode South { get; set; } = null;
            public PathNode West { get; set; } = null;
            public bool IsExit { get; set; } = false;
            public bool Domokun { get; set; } = false;
            public int Index { get; set; }

            public void BuildNodes(int index, Maze maze, int[] buildIndexes)
            {                

                Index = index;
                buildIndexes[index] = index;
                string[] currentPosition = maze.Data[index];

                IsExit = (maze.EndPoint[0] == index);
                Domokun = (maze.Domokun[0] == index);


                if (Array.IndexOf(currentPosition, "north") == -1 && index > maze.Size[0])
                {

                    if (buildIndexes[index - maze.Size[0]] != index - maze.Size[0])
                    {
                        North = new PathNode();
                        North.BuildNodes(index - maze.Size[0], maze, buildIndexes);
                    }
                    
                }

                if(index + 1 < maze.Data.Length)
                {
                    string[] nextPosition = maze.Data[index + 1];
                    if (Array.IndexOf(nextPosition, "west") == -1)
                    {
                        if (buildIndexes[index + 1] != index + 1)
                        {
                            East = new PathNode();
                            East.BuildNodes(index + 1, maze, buildIndexes);
                        }
                    }
                }

                if (index + maze.Size[0] < maze.Data.Length)
                {
                    string[] bottomPosition = maze.Data[index + maze.Size[0]];
                    if (Array.IndexOf(bottomPosition, "north") == -1)
                    {
                        if (buildIndexes[index + maze.Size[0]] != index + maze.Size[0])
                        {
                            South = new PathNode();
                            South.BuildNodes(index + maze.Size[0], maze, buildIndexes);
                        }
                    }
                }

                if (Array.IndexOf(currentPosition, "west") == -1 && index > 0)
                {
                    if (buildIndexes[index - 1] != index - 1)
                    {
                        West = new PathNode();
                        West.BuildNodes(index - 1, maze, buildIndexes);
                    }
                }

            }

            public bool PathToExit()
            {
                if (Domokun)
                {
                    return false;
                }

                if (IsExit)
                {
                    return true;
                }

                if (North != null && North.PathToExit())
                {
                    listOfSteps.Add("north");
                    return true;
                }

                if (East != null && East.PathToExit())
                {
                    listOfSteps.Add("east");
                    return true;
                }

                if (West != null && West.PathToExit())
                {
                    listOfSteps.Add("west");
                    return true;
                }

                if (South != null && South.PathToExit())
                {
                    listOfSteps.Add("south");
                    return true;
                }

                return false;

            }
        }

        public class Direction
        {
            public string direction { get; set; }
        }

        static void Main(string[] args)
        {


            RunAsync().GetAwaiter().GetResult();

        }

        static async Task RunAsync()
        {

            /*client.BaseAddress = new Uri(URL);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));*/

            await CreateMaze();
            
            Maze maze = null;

            try
            {               
                maze = await GetMazeData();                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
            }

            string input = "";
            

            if (maze != null)
            {

                Console.WriteLine("Want to play yourself or want the Pony to run through the Maze automatically? Press:");
                Console.WriteLine("1. Play yourself");
                Console.WriteLine("2. Auto-pony");
                input = Console.ReadLine();

                while (input != "1" && input != "2")
                {
                    Console.WriteLine("Try again with some valid input");
                    input = Console.ReadLine();
                }                

                if(input == "1")
                {
                    await PlayMazeByHand(maze);
                }else if(input == "2")
                {
                    await AutoPlayMaze();
                }


            }


            


        }

        static async Task AutoPlayMaze() {

            string responseCode = "active";
            Maze maze = null;
            string s = "";

            while (responseCode == "active")
            {
                maze = await GetMazeData();
                PathNode path = new PathNode();
                int[] buildIndexes = new int[maze.Data.Length];

                path.BuildNodes(maze.Pony[0], maze, buildIndexes);

                if (path.PathToExit())
                {
                    listOfSteps.Reverse();
                    s = await PostDirection(listOfSteps[0]);
                }
                else
                {
                    s = await PostDirection("stay");
                }


                
                MoveResponseObject moveResponse = JsonConvert.DeserializeObject<MoveResponseObject>(s);

                Console.WriteLine(moveResponse.State);

                responseCode = moveResponse.State;

                Console.Clear();

                Maze newMaze = await GetMazeData();

                PrintMaze(newMaze);

                Thread.Sleep(100);                
            }

            Console.Clear();
            maze = await GetMazeData();
            PrintMaze(maze);
            Console.WriteLine(s);
            Console.ReadKey();




        }

        static async Task PlayMazeByHand(Maze maze)
        {
            while (maze != null)
            {

                if (maze != null)
                {
                    PrintMaze(maze);

                }

                ConsoleKeyInfo key = Console.ReadKey();

                string direction = "";

                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:
                        direction = "east";
                        break;
                    case ConsoleKey.LeftArrow:
                        direction = "west";
                        break;
                    case ConsoleKey.UpArrow:
                        direction = "north";
                        break;
                    case ConsoleKey.DownArrow:
                        direction = "south";
                        break;
                    default:
                        direction = "stay";
                        break;
                }


                string s = await PostDirection(direction);


                Console.Clear();

                Maze newMaze = await GetMazeData();

                maze.Pony = newMaze.Pony;
                maze.Domokun = newMaze.Domokun;

            }
        }

        static void PrintMaze(Maze maze)
        {
            int width = maze.Size[0];
            int height = maze.Size[1];
            int pony = maze.Pony[0];
            int domokun = maze.Domokun[0];
            int exit = maze.EndPoint[0];
            int count = 0;

          
            /*foreach(String[] array in maze.Data)
            {
                foreach(String myString in array)
                {
                    Console.Write(myString);
                }
                Console.WriteLine();
            }*/

            for (int i = 0; i < height; i++)
            {
                for(int j = 0; j < width; j++)
                {

                    string[] square = maze.Data[(width * i) + j];

                    if(Array.IndexOf(square, "north") > -1)
                    {
                        Console.Write("+---");
                    }
                    else
                    {
                        Console.Write("+   ");
                    }

                }
                Console.Write("+  ");
                Console.WriteLine();
                for(int k = 0; k < width; k++)
                {

                    string[] square = maze.Data[(width * i) + k];

                    if (Array.IndexOf(square, "west") > -1)
                    {                       
                        if ((width * i) + k == pony)
                        {
                            Console.Write("| P ");
                        }
                        else if ((width * i) + k == exit)
                        {
                            Console.Write("| E ");
                        }
                        else if ((width * i) + k == domokun)
                        {
                            Console.Write("| D ");
                        }
                        else
                        {
                            Console.Write("|   ");
                        }
                    }
                    else
                    {
                        if ((width * i) + k == pony)
                        {
                            Console.Write("  P ");
                        }
                        else if ((width * i) + k == exit)
                        {
                            Console.Write("  E ");
                        }
                        else if ((width * i) + k == domokun)
                        {
                            Console.Write("  D ");
                        }
                        else
                        {
                            Console.Write("    ");
                        }                       
                    }

                }
                Console.Write("|  ");
                Console.WriteLine();
                
            }
            for (int j = 0; j < width; j++)
            {

                Console.Write("+---");

            }
            Console.Write("+");
            Console.WriteLine();
            //Console.WriteLine(pony);

        }

        static async Task<string> PostDirection(string direction)
        {
            

            Direction directionObject = new Direction();

            directionObject.direction = direction;

            var content = new StringContent(JsonConvert.SerializeObject(directionObject), Encoding.UTF8, "application/json");


            HttpResponseMessage response = await client.PostAsync(URL+"/"+MazeId, content); ;



            String s = "test";

            
            s = await response.Content.ReadAsStringAsync();
            

            return s;

        }

        static async Task<Maze> GetMazeData() {
            

            HttpResponseMessage response = await client.GetAsync(URL + "/" + MazeId);

            Maze maze = null;
           

            if (response.IsSuccessStatusCode)
            {
                String s = await response.Content.ReadAsStringAsync();                
                maze = JsonConvert.DeserializeObject<Maze>(s);               
            }

            return maze;

        }

        static async Task<String> CreateMaze()
        {
            MazePostObject postObject = new MazePostObject();

            postObject.Width = 15;
            postObject.Height = 25;
            postObject.PlayerName = "Rainbow Dash";
            postObject.Difficulty = 0;

            var content = new StringContent(JsonConvert.SerializeObject(postObject), Encoding.UTF8, "application/json");


            HttpResponseMessage response = await client.PostAsync(URL, content); ;



            String s = "test";


            s = await response.Content.ReadAsStringAsync();

            Console.WriteLine(s);

            MazeId = JsonConvert.DeserializeObject<MazeResponseObject>(s).Id;            

            return s;
        }
    }
}
