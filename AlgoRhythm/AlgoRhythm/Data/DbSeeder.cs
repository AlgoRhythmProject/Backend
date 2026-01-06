using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AlgoRhythm.Shared.Models.Achievements;

namespace AlgoRhythm.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        await context.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);

        var users = await SeedUsersAsync(userManager);
        await SeedContentAsync(context, users);
        await SeedAchievements(context); // Add this line
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        string[] roleNames = { "Admin", "Student" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }
    }

    private static async Task<Dictionary<string, User>> SeedUsersAsync(UserManager<User> userManager)
    {
        var users = new Dictionary<string, User>();

        // Seed admin user
        var adminEmail = "admin@algorhythm.dev";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        users["admin"] = adminUser;

        // Seed student accounts
        var sampleStudents = new[]
        {
            ("john.doe@algorhythm.dev", "John", "Doe", "john"),
            ("alice@algorhythm.dev", "Alice", "Walker", "alice"),
            ("mark@algorhythm.dev", "Mark", "Roberts", "mark")
        };

        foreach (var (email, first, last, key) in sampleStudents)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = first,
                    LastName = last,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(user, "Student123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Student");
                }
            }
            users[key] = user;
        }

        return users;
    }

    private static async Task SeedContentAsync(ApplicationDbContext context, Dictionary<string, User> users)
    {
        Console.WriteLine("Starting SeedContentAsync...");

        // Check if data already exists
        if (await context.Courses.AnyAsync())
        {
            Console.WriteLine("Data already seeded, skipping...");
            return; // Data already seeded
        }

        Console.WriteLine("No existing data found, proceeding with seeding...");

        // --- TAGS ---
        var tagAlgo = new Tag { Name = "Algorithms", Description = "Algorithmic thinking and problem solving" };
        var tagIntro = new Tag { Name = "Introduction", Description = "Beginner friendly content" };
        var tagCSharp = new Tag { Name = "CSharp", Description = "C# programming tasks and concepts" };
        var tagData = new Tag { Name = "DataStructures", Description = "Data structure fundamentals" };
        var tagArrays = new Tag { Name = "Arrays", Description = "Array manipulation and techniques" };
        var tagTrees = new Tag { Name = "Trees", Description = "Tree structures and algorithms" };
        var tagGraphs = new Tag { Name = "Graphs", Description = "Graph theory and algorithms" };
        var tagStrings = new Tag { Name = "Strings", Description = "String manipulation" };
        var tagDP = new Tag { Name = "DynamicProgramming", Description = "Dynamic programming patterns" };
        var tagSorting = new Tag { Name = "Sorting", Description = "Sorting algorithms" };
        var tagStack = new Tag { Name = "Stack", Description = "Stack data structure" };
        var tagQueue = new Tag { Name = "Queue", Description = "Queue data structure" };
        var tagLinkedList = new Tag { Name = "LinkedList", Description = "Linked list structures" };

        await context.Tags.AddRangeAsync(tagAlgo, tagIntro, tagCSharp, tagData, tagArrays, tagTrees,
            tagGraphs, tagStrings, tagDP, tagSorting, tagStack, tagQueue, tagLinkedList);

        // --- COURSES ---
        var course1 = new Course
        {
            Name = "C# Programming Fundamentals",
            Description = "Learn the fundamentals of C#, including variables, loops, conditionals, and simple algorithms. Perfect for beginners starting their programming journey.",
            CreatedAt = DateTime.UtcNow,
            IsPublished = true
        };

        var course2 = new Course
        {
            Name = "Data Structures Essentials",
            Description = "Master essential data structures including arrays, linked lists, stacks, queues, and trees. Learn when and how to use each structure effectively.",
            CreatedAt = DateTime.UtcNow,
            IsPublished = true
        };

        var course3 = new Course
        {
            Name = "Advanced Algorithms",
            Description = "Deep dive into complex algorithms including graph theory, dynamic programming, and advanced sorting techniques. For experienced programmers.",
            CreatedAt = DateTime.UtcNow,
            IsPublished = true
        };

        await context.Courses.AddRangeAsync(course1, course2, course3);

        // --- LECTURES FOR COURSE 1 (C# Programming Fundamentals) ---
        var lec1_1 = new Lecture { Title = "Welcome to C# Programming", Course = course1 };
        var lec1_2 = new Lecture { Title = "Variables and Data Types", Course = course1 };
        var lec1_3 = new Lecture { Title = "Control Flow: Loops and Conditions", Course = course1 };
        var lec1_4 = new Lecture { Title = "Functions and Methods", Course = course1 };
        var lec1_5 = new Lecture { Title = "Introduction to Arrays", Course = course1 };

        // --- LECTURES FOR COURSE 2 (Data Structures Essentials) ---
        var lec2_1 = new Lecture { Title = "Arrays Deep Dive", Course = course2 };
        var lec2_2 = new Lecture { Title = "Two Pointer Technique", Course = course2 };
        var lec2_3 = new Lecture { Title = "Stack Fundamentals", Course = course2 };
        var lec2_4 = new Lecture { Title = "Queue Implementation", Course = course2 };
        var lec2_5 = new Lecture { Title = "Linked Lists Introduction", Course = course2 };
        var lec2_6 = new Lecture { Title = "Binary Trees Basics", Course = course2 };

        // --- LECTURES FOR COURSE 3 (Advanced Algorithms) ---
        var lec3_1 = new Lecture { Title = "Graph Representation", Course = course3 };
        var lec3_2 = new Lecture { Title = "Dynamic Programming Introduction", Course = course3 };
        var lec3_3 = new Lecture { Title = "Advanced Sorting Algorithms", Course = course3 };
        var lec3_4 = new Lecture { Title = "Divide and Conquer", Course = course3 };

        await context.Lectures.AddRangeAsync(
            lec1_1, lec1_2, lec1_3, lec1_4, lec1_5,
            lec2_1, lec2_2, lec2_3, lec2_4, lec2_5, lec2_6,
            lec3_1, lec3_2, lec3_3, lec3_4
        );

        // --- LECTURE CONTENT ---

        // Course 1 Content
        var content1_1 = new LectureText
        {
            Lecture = lec1_1,
            HtmlContent = @"<h1>Welcome to C# Programming</h1>
<p>C# is a modern, object-oriented programming language developed by Microsoft. It's widely used for building desktop applications, web services, games, and more.</p>
<h2>What You'll Learn</h2>
<p>In this course, you'll master the fundamentals of C# programming including:</p>
<ul>
<li>Variables and data types</li>
<li>Control flow structures</li>
<li>Functions and methods</li>
<li>Basic data structures</li>
</ul>
<h2>Why Learn C#?</h2>
<p>C# is one of the most popular programming languages, known for its:</p>
<ul>
<li><strong>Strong typing</strong> - catches errors at compile time</li>
<li><strong>Rich ecosystem</strong> - extensive libraries and frameworks</li>
<li><strong>Cross-platform support</strong> - runs on Windows, Linux, and macOS</li>
<li><strong>Career opportunities</strong> - high demand in the job market</li>
</ul>",
            Type = ContentType.Text
        };

        var content1_2 = new LectureText
        {
            Lecture = lec1_2,
            HtmlContent = @"<h1>Variables and Data Types</h1>
<p>Variables are containers for storing data values. In C#, every variable must be declared with a specific data type.</p>
<h2>Common Data Types</h2>
<h3>Numeric Types</h3>
<ul>
<li><code>int</code> - whole numbers (e.g., 42, -17)</li>
<li><code>double</code> - decimal numbers (e.g., 3.14, -0.5)</li>
<li><code>decimal</code> - precise decimal numbers for financial calculations</li>
</ul>
<h3>Text Types</h3>
<ul>
<li><code>string</code> - text (e.g., ""Hello, World!"")</li>
<li><code>char</code> - single character (e.g., 'A')</li>
</ul>
<h3>Boolean Type</h3>
<ul>
<li><code>bool</code> - true or false values</li>
</ul>
<h2>Variable Declaration</h2>
<pre><code>int age = 25;
string name = ""John"";
bool isStudent = true;
double gpa = 3.85;</code></pre>
<h2>Type Inference with 'var'</h2>
<p>C# can automatically infer the type:</p>
<pre><code>var age = 25; // compiler knows it's an int
var name = ""John""; // compiler knows it's a string</code></pre>",
            Type = ContentType.Text
        };

        var content1_3 = new LectureText
        {
            Lecture = lec1_3,
            HtmlContent = @"<h1>Control Flow: Loops and Conditions</h1>
<p>Control flow structures allow your program to make decisions and repeat actions.</p>
<h2>Conditional Statements</h2>
<h3>If-Else Statement</h3>
<pre><code>if (age >= 18)
{
    Console.WriteLine(""You are an adult"");
}
else
{
    Console.WriteLine(""You are a minor"");
}</code></pre>
<h3>Switch Statement</h3>
<pre><code>switch (dayOfWeek)
{
    case 1:
        Console.WriteLine(""Monday"");
        break;
    case 2:
        Console.WriteLine(""Tuesday"");
        break;
    default:
        Console.WriteLine(""Other day"");
        break;
}</code></pre>
<h2>Loops</h2>
<h3>For Loop</h3>
<pre><code>for (int i = 0; i < 10; i++)
{
    Console.WriteLine(i);
}</code></pre>
<h3>While Loop</h3>
<pre><code>int count = 0;
while (count < 5)
{
    Console.WriteLine(count);
    count++;
}</code></pre>
<h3>Foreach Loop</h3>
<pre><code>int[] numbers = { 1, 2, 3, 4, 5 };
foreach (int num in numbers)
{
    Console.WriteLine(num);
}</code></pre>",
            Type = ContentType.Text
        };

        var content1_5 = new LectureText
        {
            Lecture = lec1_5,
            HtmlContent = @"<h1>Introduction to Arrays</h1>
<p>Arrays are data structures that store multiple values of the same type in a single variable.</p>
<h2>Declaring Arrays</h2>
<pre><code>// Declaration and initialization
int[] numbers = new int[5];

// With initial values
int[] scores = { 90, 85, 92, 88, 95 };

// Using new keyword
string[] names = new string[] { ""Alice"", ""Bob"", ""Charlie"" };</code></pre>
<h2>Accessing Elements</h2>
<p>Array indices start at 0:</p>
<pre><code>int[] numbers = { 10, 20, 30, 40, 50 };
Console.WriteLine(numbers[0]); // Output: 10
Console.WriteLine(numbers[2]); // Output: 30

// Modifying elements
numbers[1] = 25;
Console.WriteLine(numbers[1]); // Output: 25</code></pre>
<h2>Array Properties</h2>
<pre><code>int[] numbers = { 1, 2, 3, 4, 5 };
Console.WriteLine(numbers.Length); // Output: 5</code></pre>
<h2>Common Array Operations</h2>
<h3>Iterating Through an Array</h3>
<pre><code>foreach (int num in numbers)
{
    Console.WriteLine(num);
}</code></pre>
<h3>Finding Elements</h3>
<pre><code>int index = Array.IndexOf(numbers, 3); // Returns 2
bool contains = numbers.Contains(4); // Returns true</code></pre>",
            Type = ContentType.Text
        };

        // Course 2 Content
        var content2_1 = new LectureText
        {
            Lecture = lec2_1,
            HtmlContent = @"<h1>Arrays Deep Dive</h1>
<p>Arrays are one of the most fundamental data structures in computer science. Understanding arrays deeply is crucial for efficient programming.</p>
<h2>Time Complexity</h2>
<ul>
<li><strong>Access</strong>: O(1) - Direct access by index</li>
<li><strong>Search</strong>: O(n) - May need to check every element</li>
<li><strong>Insert</strong>: O(n) - May need to shift elements</li>
<li><strong>Delete</strong>: O(n) - May need to shift elements</li>
</ul>
<h2>Advantages</h2>
<ul>
<li>Fast random access</li>
<li>Cache-friendly (contiguous memory)</li>
<li>Simple and intuitive</li>
</ul>
<h2>Disadvantages</h2>
<ul>
<li>Fixed size (in most languages)</li>
<li>Expensive insertions/deletions</li>
<li>Memory waste if not fully utilized</li>
</ul>
<h2>Common Patterns</h2>
<h3>Prefix Sum</h3>
<p>Useful for range sum queries:</p>
<pre><code>int[] arr = { 1, 2, 3, 4, 5 };
int[] prefixSum = new int[arr.Length];
prefixSum[0] = arr[0];
for (int i = 1; i < arr.Length; i++)
{
    prefixSum[i] = prefixSum[i-1] + arr[i];
}</code></pre>",
            Type = ContentType.Text
        };

        var content2_2 = new LectureText
        {
            Lecture = lec2_2,
            HtmlContent = @"<h1>Two Pointer Technique</h1>
<p>The two pointer technique is a powerful pattern for solving array and string problems efficiently.</p>
<h2>When to Use</h2>
<ul>
<li>Working with sorted arrays</li>
<li>Finding pairs with specific properties</li>
<li>Problems requiring comparison of elements</li>
<li>Need to optimize from O(n²) to O(n)</li>
</ul>
<h2>Types of Two Pointer Approaches</h2>
<h3>1. Opposite Direction</h3>
<p>Pointers start at opposite ends:</p>
<pre><code>int left = 0;
int right = arr.Length - 1;

while (left < right)
{
    // Process
    if (condition)
        left++;
    else
        right--;
}</code></pre>
<h3>2. Same Direction (Slow-Fast)</h3>
<p>Both pointers move in same direction:</p>
<pre><code>int slow = 0;
int fast = 0;

while (fast < arr.Length)
{
    if (condition)
    {
        arr[slow] = arr[fast];
        slow++;
    }
    fast++;
}</code></pre>
<h2>Example: Remove Duplicates</h2>
<pre><code>public int RemoveDuplicates(int[] nums)
{
    if (nums.Length == 0) return 0;
    
    int slow = 0;
    for (int fast = 1; fast < nums.Length; fast++)
    {
        if (nums[fast] != nums[slow])
        {
            slow++;
            nums[slow] = nums[fast];
        }
    }
    return slow + 1;
}</code></pre>",
            Type = ContentType.Text
        };

        var content2_3 = new LectureText
        {
            Lecture = lec2_3,
            HtmlContent = @"<h1>Stack Fundamentals</h1>
<p>A stack is a linear data structure following the Last In First Out (LIFO) principle.</p>
<h2>Core Operations</h2>
<ul>
<li><strong>Push</strong>: Add element to top - O(1)</li>
<li><strong>Pop</strong>: Remove element from top - O(1)</li>
<li><strong>Peek/Top</strong>: View top element - O(1)</li>
<li><strong>IsEmpty</strong>: Check if empty - O(1)</li>
</ul>
<h2>Implementation in C#</h2>
<pre><code>Stack<int> stack = new Stack<int>();

// Push elements
stack.Push(1);
stack.Push(2);
stack.Push(3);

// Peek at top
int top = stack.Peek(); // Returns 3, doesn't remove

// Pop element
int popped = stack.Pop(); // Returns and removes 3

// Check if empty
bool isEmpty = stack.Count == 0;</code></pre>
<h2>Common Use Cases</h2>
<ul>
<li>Function call stack</li>
<li>Undo/Redo functionality</li>
<li>Expression evaluation</li>
<li>Backtracking algorithms</li>
<li>Depth-first search (DFS)</li>
</ul>
<h2>Example: Balanced Parentheses</h2>
<pre><code>public bool IsValid(string s)
{
    Stack<char> stack = new Stack<char>();
    Dictionary<char, char> pairs = new Dictionary<char, char>
    {
        {')', '('}, {'}', '{'}, {']', '['}
    };
    
    foreach (char c in s)
    {
        if (pairs.ContainsValue(c))
        {
            stack.Push(c);
        }
        else if (pairs.ContainsKey(c))
        {
            if (stack.Count == 0 || stack.Pop() != pairs[c])
                return false;
        }
    }
    
    return stack.Count == 0;
}</code></pre>",
            Type = ContentType.Text
        };

        var content2_6 = new LectureText
        {
            Lecture = lec2_6,
            HtmlContent = @"<h1>Binary Trees Basics</h1>
<p>A binary tree is a hierarchical data structure where each node has at most two children.</p>
<h2>Tree Terminology</h2>
<ul>
<li><strong>Root</strong>: Topmost node</li>
<li><strong>Parent</strong>: Node with children</li>
<li><strong>Child</strong>: Node descending from parent</li>
<li><strong>Leaf</strong>: Node with no children</li>
<li><strong>Height</strong>: Longest path from node to leaf</li>
<li><strong>Depth</strong>: Path length from root to node</li>
</ul>
<h2>Node Structure</h2>
<pre><code>public class TreeNode
{
    public int Value { get; set; }
    public TreeNode Left { get; set; }
    public TreeNode Right { get; set; }
    
    public TreeNode(int val)
    {
        Value = val;
        Left = null;
        Right = null;
    }
}</code></pre>
<h2>Tree Traversals</h2>
<h3>Inorder (Left, Root, Right)</h3>
<pre><code>void Inorder(TreeNode node)
{
    if (node == null) return;
    Inorder(node.Left);
    Console.WriteLine(node.Value);
    Inorder(node.Right);
}</code></pre>
<h3>Preorder (Root, Left, Right)</h3>
<pre><code>void Preorder(TreeNode node)
{
    if (node == null) return;
    Console.WriteLine(node.Value);
    Preorder(node.Left);
    Preorder(node.Right);
}</code></pre>
<h3>Postorder (Left, Right, Root)</h3>
<pre><code>void Postorder(TreeNode node)
{
    if (node == null) return;
    Postorder(node.Left);
    Postorder(node.Right);
    Console.WriteLine(node.Value);
}</code></pre>",
            Type = ContentType.Text
        };

        // Course 3 Content
        var content3_1 = new LectureText
        {
            Lecture = lec3_1,
            HtmlContent = @"<h1>Graph Representation</h1>
<p>Graphs consist of vertices (nodes) connected by edges, representing relationships and networks.</p>
<h2>Graph Types</h2>
<h3>Directed vs Undirected</h3>
<ul>
<li><strong>Directed</strong>: Edges have direction (A → B)</li>
<li><strong>Undirected</strong>: Edges are bidirectional (A ↔ B)</li>
</ul>
<h3>Weighted vs Unweighted</h3>
<ul>
<li><strong>Weighted</strong>: Edges have costs/values</li>
<li><strong>Unweighted</strong>: All edges equal</li>
</ul>
<h2>Representation Methods</h2>
<h3>Adjacency Matrix</h3>
<pre><code>// 2D array where matrix[i][j] = 1 if edge exists
int[,] graph = new int[n, n];
graph[0, 1] = 1; // Edge from 0 to 1</code></pre>
<p><strong>Space</strong>: O(V²) | <strong>Edge lookup</strong>: O(1)</p>
<h3>Adjacency List</h3>
<pre><code>// Dictionary of lists
Dictionary<int, List<int>> graph = new Dictionary<int, List<int>>();
graph[0] = new List<int> { 1, 2 }; // 0 connects to 1 and 2</code></pre>
<p><strong>Space</strong>: O(V + E) | <strong>Edge lookup</strong>: O(V)</p>
<h2>Graph Traversals</h2>
<h3>BFS (Breadth-First Search)</h3>
<pre><code>void BFS(int start, Dictionary<int, List<int>> graph)
{
    Queue<int> queue = new Queue<int>();
    HashSet<int> visited = new HashSet<int>();
    
    queue.Enqueue(start);
    visited.Add(start);
    
    while (queue.Count > 0)
    {
        int node = queue.Dequeue();
        Console.WriteLine(node);
        
        foreach (int neighbor in graph[node])
        {
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }
    }
}</code></pre>",
            Type = ContentType.Text
        };

        var content3_2 = new LectureText
        {
            Lecture = lec3_2,
            HtmlContent = @"<h1>Dynamic Programming Introduction</h1>
<p>Dynamic Programming (DP) is an optimization technique that solves complex problems by breaking them into simpler subproblems.</p>
<h2>When to Use DP</h2>
<ul>
<li><strong>Optimal Substructure</strong>: Solution can be built from subproblem solutions</li>
<li><strong>Overlapping Subproblems</strong>: Same subproblems solved multiple times</li>
</ul>
<h2>DP Approaches</h2>
<h3>1. Top-Down (Memoization)</h3>
<p>Recursive approach with caching:</p>
<pre><code>Dictionary<int, int> memo = new Dictionary<int, int>();

int Fibonacci(int n)
{
    if (n <= 1) return n;
    if (memo.ContainsKey(n)) return memo[n];
    
    memo[n] = Fibonacci(n-1) + Fibonacci(n-2);
    return memo[n];
}</code></pre>
<h3>2. Bottom-Up (Tabulation)</h3>
<p>Iterative approach building from base cases:</p>
<pre><code>int Fibonacci(int n)
{
    if (n <= 1) return n;
    
    int[] dp = new int[n + 1];
    dp[0] = 0;
    dp[1] = 1;
    
    for (int i = 2; i <= n; i++)
    {
        dp[i] = dp[i-1] + dp[i-2];
    }
    
    return dp[n];
}</code></pre>
<h2>Classic DP Problems</h2>
<ul>
<li>Fibonacci sequence</li>
<li>Longest common subsequence</li>
<li>Knapsack problem</li>
<li>Coin change</li>
<li>Edit distance</li>
</ul>",
            Type = ContentType.Text
        };

        await context.LectureContents.AddRangeAsync(
            content1_1, content1_2, content1_3, content1_5,
            content2_1, content2_2, content2_3, content2_6,
            content3_1, content3_2
        );

        // --- LECTURE-TAG RELATIONSHIPS ---
        // Course 1 - C# Programming Fundamentals
        lec1_1.Tags = new List<Tag> { tagIntro, tagCSharp };
        lec1_2.Tags = new List<Tag> { tagCSharp };
        lec1_3.Tags = new List<Tag> { tagCSharp };
        lec1_4.Tags = new List<Tag> { tagCSharp };
        lec1_5.Tags = new List<Tag> { tagArrays, tagIntro };

        // Course 2 - Data Structures Essentials
        lec2_1.Tags = new List<Tag> { tagArrays, tagData };
        lec2_2.Tags = new List<Tag> { tagArrays, tagAlgo };
        lec2_3.Tags = new List<Tag> { tagStack, tagData };
        lec2_4.Tags = new List<Tag> { tagQueue, tagData };
        lec2_5.Tags = new List<Tag> { tagLinkedList, tagData };
        lec2_6.Tags = new List<Tag> { tagTrees, tagData };

        // Course 3 - Advanced Algorithms
        lec3_1.Tags = new List<Tag> { tagGraphs, tagAlgo };
        lec3_2.Tags = new List<Tag> { tagDP, tagAlgo };
        lec3_3.Tags = new List<Tag> { tagSorting, tagAlgo };
        lec3_4.Tags = new List<Tag> { tagAlgo };

        // --- PROGRAMMING TASKS ---
        var task1 = new ProgrammingTaskItem
        {
            Title = "Sum of Two Numbers",
            Description = "Write a function that returns the sum of two integers. This is a simple warm-up exercise to get familiar with basic C# syntax and the testing environment.",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class Solution 
{ 
    public int Sum(int a, int b) 
    { 
        // Write your solution here
        return 0; 
    } 
}"
        };

        var task2 = new ProgrammingTaskItem
        {
            Title = "Find Maximum",
            Description = "Given two integers, return the larger one. If they are equal, return either value.",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class Solution 
{ 
    public int Max(int a, int b) 
    { 
        // Write your solution here
        return 0; 
    } 
}"
        };

        var task3 = new ProgrammingTaskItem
        {
            Title = "Reverse String",
            Description = @"Given a string, return a new string with characters in reverse order. 
You must implement this without using built-in reverse methods.

Example:
Input: ""hello""
Output: ""olleh""",
            Difficulty = Difficulty.Medium,
            TemplateCode = @"public class Solution 
{ 
    public string Reverse(string s) 
    { 
        // Write your solution here
        return """"; 
    } 
}"
        };

        var task4 = new ProgrammingTaskItem
        {
            Title = "Two Sum",
            Description = @"Given an array of integers nums and an integer target, return indices of the two numbers that add up to target.
You may assume that each input has exactly one solution, and you may not use the same element twice.

Example:
Input: nums = [2,7,11,15], target = 9
Output: [0,1]
Explanation: nums[0] + nums[1] = 2 + 7 = 9",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class Solution 
{ 
    public int[] TwoSum(int[] nums, int target) 
    { 
        // Write your solution here
        return new int[0]; 
    } 
}"
        };

        var task5 = new ProgrammingTaskItem
        {
            Title = "Valid Parentheses",
            Description = @"Given a string containing just the characters '(', ')', '{', '}', '[' and ']', determine if the input string is valid.
An input string is valid if:
1. Open brackets are closed by the same type of brackets
2. Open brackets are closed in the correct order

Example:
Input: ""()[]{}""
Output: true

Input: ""([)]""
Output: false",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class Solution 
{ 
    public bool IsValid(string s) 
    { 
        // Write your solution here
        return false; 
    } 
}"
        };

        var task6 = new ProgrammingTaskItem
        {
            Title = "Merge Two Sorted Arrays",
            Description = @"You are given two integer arrays nums1 and nums2, sorted in non-decreasing order. 
Merge nums2 into nums1 as one sorted array.

Example:
Input: nums1 = [1,2,3,0,0,0], m = 3, nums2 = [2,5,6], n = 3
Output: [1,2,2,3,5,6]",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class Solution 
{ 
    public void Merge(int[] nums1, int m, int[] nums2, int n) 
    { 
        // Write your solution here
    } 
}"
        };

        var task7 = new ProgrammingTaskItem
        {
            Title = "Binary Tree Level Order Traversal",
            Description = @"Given the root of a binary tree, return the level order traversal of its nodes' values. 
(i.e., from left to right, level by level).

Example:
Input: root = [3,9,20,null,null,15,7]
Output: [[3],[9,20],[15,7]]",
            Difficulty = Difficulty.Medium,
            TemplateCode = @"public class Solution 
{ 
    public IList<IList<int>> LevelOrder(TreeNode root) 
    { 
        // Write your solution here
        return new List<IList<int>>(); 
    } 
}"
        };

        var task8 = new ProgrammingTaskItem
        {
            Title = "Longest Palindromic Substring",
            Description = @"Given a string s, return the longest palindromic substring in s.

A palindrome is a string that reads the same forward and backward.

Example:
Input: s = ""babad""
Output: ""bab"" (Note: ""aba"" is also a valid answer)

Input: s = ""cbbd""
Output: ""bb""",
            Difficulty = Difficulty.Medium,
            TemplateCode = @"public class Solution 
{ 
    public string LongestPalindrome(string s) 
    { 
        // Write your solution here
        return """"; 
    } 
}"
        };

        var task9 = new ProgrammingTaskItem
        {
            Title = "Implement Stack using Queues",
            Description = @"Implement a last-in-first-out (LIFO) stack using only two queues.

Implement the MyStack class:
- void Push(int x) Pushes element x to the top of the stack
- int Pop() Removes the element on the top of the stack and returns it
- int Top() Returns the element on the top of the stack
- bool Empty() Returns true if the stack is empty, false otherwise",
            Difficulty = Difficulty.Easy,
            TemplateCode = @"public class MyStack 
{
    public MyStack() 
    {
        
    }
    
    public void Push(int x) 
    {
        
    }
    
    public int Pop() 
    {
        return 0;
    }
    
    public int Top() 
    {
        return 0;
    }
    
    public bool Empty() 
    {
        return true;
    }
}"
        };

        var task10 = new ProgrammingTaskItem
        {
            Title = "Find First Missing Positive",
            Description = @"Given an unsorted integer array nums, return the smallest missing positive integer.

You must implement an algorithm that runs in O(n) time and uses O(1) auxiliary space.

Example:
Input: nums = [1,2,0]
Output: 3

Input: nums = [3,4,-1,1]
Output: 2

Input: nums = [7,8,9,11,12]
Output: 1",
            Difficulty = Difficulty.Hard,
            TemplateCode = @"public class Solution 
{ 
    public int FirstMissingPositive(int[] nums) 
    { 
        // Write your solution here
        return 0; 
    } 
}"
        };

        var task11 = new ProgrammingTaskItem
        {
            Title = "Graph Valid Tree",
            Description = @"Given n nodes labeled from 0 to n-1 and a list of undirected edges, check if these edges form a valid tree.

A valid tree must:
1. Be connected (all nodes reachable from any node)
2. Have no cycles
3. Have exactly n-1 edges

Example:
Input: n = 5, edges = [[0,1], [0,2], [0,3], [1,4]]
Output: true

Input: n = 5, edges = [[0,1], [1,2], [2,3], [1,3], [1,4]]
Output: false",
            Difficulty = Difficulty.Medium,
            TemplateCode = @"public class Solution 
{ 
    public bool ValidTree(int n, int[][] edges) 
    { 
        // Write your solution here
        return false; 
    } 
}"
        };

        var task12 = new ProgrammingTaskItem
        {
            Title = "Coin Change",
            Description = @"You are given an integer array coins representing coins of different denominations and an integer amount.
Return the fewest number of coins needed to make up that amount. If that amount cannot be made up, return -1.

You may assume you have an infinite number of each coin.

Example:
Input: coins = [1,2,5], amount = 11
Output: 3 (11 = 5 + 5 + 1)

Input: coins = [2], amount = 3
Output: -1",
            Difficulty = Difficulty.Medium,
            TemplateCode = @"public class Solution 
{ 
    public int CoinChange(int[] coins, int amount) 
    { 
        // Write your solution here
        return 0; 
    } 
}"
        };

        // --- INTERACTIVE TASKS ---
        var task13 = new InteractiveTaskItem
        {
            Title = "Guess the Output: Loops",
            Description = "Look at the following C# code and choose the correct output.",
            Difficulty = Difficulty.Easy
        };

        var task14 = new InteractiveTaskItem
        {
            Title = "Spot the Bug: Array Access",
            Description = "Identify the bug in the given code snippet.",
            Difficulty = Difficulty.Easy
        };

        // Add hints after creating tasks and before test cases

        // --- HINTS ---
        var hint1 = new Hint
        {
            TaskItem = task1,
            Content = "Use the + operator (shift and = ) to add two numbers together and return the result.",
            Order = 1
        };

        var hint2 = new Hint
        {
            TaskItem = task2,
            Content = "Use the ternary operator: return (a > b) ? a : b; or a simple if-else statement.",
            Order = 1
        };

        var hint3 = new Hint
        {
            TaskItem = task3,
            Content = "Create a character array from the string, reverse it using a loop with two pointers (start and end), then convert back to string.",
            Order = 1
        };

        var hint4 = new Hint
        {
            TaskItem = task4,
            Content = "Use a Dictionary<int, int> to store numbers you've seen. For each number, check if (target - number) exists in the dictionary.",
            Order = 1
        };

        var hint5 = new Hint
        {
            TaskItem = task5,
            Content = "Use a Stack<char>. Push opening brackets onto the stack. When you see a closing bracket, pop from stack and check if they match.",
            Order = 1
        };

        var hint6 = new Hint
        {
            TaskItem = task6,
            Content = "Start from the end of both arrays and work backwards. Compare elements and place the larger one at the end of nums1.",
            Order = 1
        };

        var hint7 = new Hint
        {
            TaskItem = task7,
            Content = "Use a Queue<TreeNode> for BFS (Breadth-First Search). Process nodes level by level, keeping track of the current level size.",
            Order = 1
        };

        var hint8 = new Hint
        {
            TaskItem = task8,
            Content = "For each character, expand around it treating it as the center of a palindrome. Check both odd-length (single center) and even-length (two centers) palindromes.",
            Order = 1
        };

        var hint9 = new Hint
        {
            TaskItem = task9,
            Content = "Use two queues. For Push, add to queue1. For Pop, move all elements except the last from queue1 to queue2, then swap the queues.",
            Order = 1
        };

        var hint10 = new Hint
        {
            TaskItem = task10,
            Content = "Place each positive number at its correct index (number n goes to index n-1). Then scan the array to find the first index where the number doesn't match.",
            Order = 1
        };

        var hint11 = new Hint
        {
            TaskItem = task11,
            Content = "A valid tree must have exactly n-1 edges and be fully connected. Use DFS or BFS to check connectivity, and verify edge count.",
            Order = 1
        };

        var hint12 = new Hint
        {
            TaskItem = task12,
            Content = "Use dynamic programming. Create an array dp where dp[i] represents the minimum coins needed for amount i. For each amount, try all coin denominations.",
            Order = 1
        };

        var hint13 = new Hint
        {
            TaskItem = task13,
            Content = "Carefully trace through each loop iteration, keeping track of variable values at each step.",
            Order = 1
        };

        var hint14 = new Hint
        {
            TaskItem = task14,
            Content = "Check if the array index is within valid bounds (0 to array.Length - 1) before accessing elements.",
            Order = 1
        };

        await context.Hints.AddRangeAsync(
            hint1, hint2, hint3, hint4, hint5, hint6, hint7,
            hint8, hint9, hint10, hint11, hint12, hint13, hint14
        );


        // --- TEST CASES ---
        var test1 = new TestCase
        {
            ProgrammingTaskItem = task1,
            InputJson = "{ \"a\": 1, \"b\": 2 }",
            ExpectedJson = "3",
            IsVisible = true
        };

        var test1_2 = new TestCase
        {
            ProgrammingTaskItem = task1,
            InputJson = "{ \"a\": -5, \"b\": 10 }",
            ExpectedJson = "5",
            IsVisible = true
        };

        var test1_3 = new TestCase
        {
            ProgrammingTaskItem = task1,
            InputJson = "{ \"a\": 0, \"b\": 0 }",
            ExpectedJson = "0",
            IsVisible = false
        };

        var test2 = new TestCase
        {
            ProgrammingTaskItem = task2,
            InputJson = "{ \"a\": 5, \"b\": 3 }",
            ExpectedJson = "5",
            IsVisible = true
        };

        var test2_2 = new TestCase
        {
            ProgrammingTaskItem = task2,
            InputJson = "{ \"a\": -10, \"b\": -5 }",
            ExpectedJson = "-5",
            IsVisible = true
        };

        var test2_3 = new TestCase
        {
            ProgrammingTaskItem = task2,
            InputJson = "{ \"a\": 7, \"b\": 7 }",
            ExpectedJson = "7",
            IsVisible = false
        };

        var test3 = new TestCase
        {
            ProgrammingTaskItem = task3,
            InputJson = "{ \"s\": \"hello\" }",
            ExpectedJson = "\"olleh\"",
            IsVisible = true
        };

        var test3_2 = new TestCase
        {
            ProgrammingTaskItem = task3,
            InputJson = "{ \"s\": \"world\" }",
            ExpectedJson = "\"dlrow\"",
            IsVisible = true
        };

        var test3_3 = new TestCase
        {
            ProgrammingTaskItem = task3,
            InputJson = "{ \"s\": \"a\" }",
            ExpectedJson = "\"a\"",
            IsVisible = false
        };

        var test3_4 = new TestCase
        {
            ProgrammingTaskItem = task3,
            InputJson = "{ \"s\": \"\" }",
            ExpectedJson = "\"\"",
            IsVisible = false
        };

        var test4 = new TestCase
        {
            ProgrammingTaskItem = task4,
            InputJson = "{ \"nums\": [2,7,11,15], \"target\": 9 }",
            ExpectedJson = "[0,1]",
            IsVisible = true
        };

        var test4_2 = new TestCase
        {
            ProgrammingTaskItem = task4,
            InputJson = "{ \"nums\": [3,2,4], \"target\": 6 }",
            ExpectedJson = "[1,2]",
            IsVisible = true
        };

        var test4_3 = new TestCase
        {
            ProgrammingTaskItem = task4,
            InputJson = "{ \"nums\": [3,3], \"target\": 6 }",
            ExpectedJson = "[0,1]",
            IsVisible = false
        };

        var test5 = new TestCase
        {
            ProgrammingTaskItem = task5,
            InputJson = "{ \"s\": \"()\" }",
            ExpectedJson = "true",
            IsVisible = true
        };

        var test5_2 = new TestCase
        {
            ProgrammingTaskItem = task5,
            InputJson = "{ \"s\": \"()[]{}\" }",
            ExpectedJson = "true",
            IsVisible = true
        };

        var test5_3 = new TestCase
        {
            ProgrammingTaskItem = task5,
            InputJson = "{ \"s\": \"(]\" }",
            ExpectedJson = "false",
            IsVisible = true
        };

        var test5_4 = new TestCase
        {
            ProgrammingTaskItem = task5,
            InputJson = "{ \"s\": \"([)]\" }",
            ExpectedJson = "false",
            IsVisible = false
        };

        var test6 = new TestCase
        {
            ProgrammingTaskItem = task6,
            InputJson = "{ \"nums1\": [1,2,3,0,0,0], \"m\": 3, \"nums2\": [2,5,6], \"n\": 3 }",
            ExpectedJson = "[1,2,2,3,5,6]",
            IsVisible = true
        };

        var test6_2 = new TestCase
        {
            ProgrammingTaskItem = task6,
            InputJson = "{ \"nums1\": [1], \"m\": 1, \"nums2\": [], \"n\": 0 }",
            ExpectedJson = "[1]",
            IsVisible = false
        };

        var test7 = new TestCase
        {
            ProgrammingTaskItem = task7,
            InputJson = "{ \"root\": [3,9,20,null,null,15,7] }",
            ExpectedJson = "[[3],[9,20],[15,7]]",
            IsVisible = true
        };

        var test7_2 = new TestCase
        {
            ProgrammingTaskItem = task7,
            InputJson = "{ \"root\": [1] }",
            ExpectedJson = "[[1]]",
            IsVisible = true
        };

        var test7_3 = new TestCase
        {
            ProgrammingTaskItem = task7,
            InputJson = "{ \"root\": [] }",
            ExpectedJson = "[]",
            IsVisible = false
        };

        var test8 = new TestCase
        {
            ProgrammingTaskItem = task8,
            InputJson = "{ \"s\": \"babad\" }",
            ExpectedJson = "\"bab\"",
            IsVisible = true
        };

        var test8_2 = new TestCase
        {
            ProgrammingTaskItem = task8,
            InputJson = "{ \"s\": \"cbbd\" }",
            ExpectedJson = "\"bb\"",
            IsVisible = true
        };

        var test8_3 = new TestCase
        {
            ProgrammingTaskItem = task8,
            InputJson = "{ \"s\": \"a\" }",
            ExpectedJson = "\"a\"",
            IsVisible = false
        };

        var test10 = new TestCase
        {
            ProgrammingTaskItem = task10,
            InputJson = "{ \"nums\": [1,2,0] }",
            ExpectedJson = "3",
            IsVisible = true
        };

        var test10_2 = new TestCase
        {
            ProgrammingTaskItem = task10,
            InputJson = "{ \"nums\": [3,4,-1,1] }",
            ExpectedJson = "2",
            IsVisible = true
        };

        var test10_3 = new TestCase
        {
            ProgrammingTaskItem = task10,
            InputJson = "{ \"nums\": [7,8,9,11,12] }",
            ExpectedJson = "1",
            IsVisible = false
        };

        var test11 = new TestCase
        {
            ProgrammingTaskItem = task11,
            InputJson = "{ \"n\": 5, \"edges\": [[0,1],[0,2],[0,3],[1,4]] }",
            ExpectedJson = "true",
            IsVisible = true
        };

        var test11_2 = new TestCase
        {
            ProgrammingTaskItem = task11,
            InputJson = "{ \"n\": 5, \"edges\": [[0,1],[1,2],[2,3],[1,3],[1,4]] }",
            ExpectedJson = "false",
            IsVisible = true
        };

        var test11_3 = new TestCase
        {
            ProgrammingTaskItem = task11,
            InputJson = "{ \"n\": 1, \"edges\": [] }",
            ExpectedJson = "true",
            IsVisible = false
        };

        var test12 = new TestCase
        {
            ProgrammingTaskItem = task12,
            InputJson = "{ \"coins\": [1,2,5], \"amount\": 11 }",
            ExpectedJson = "3",
            IsVisible = true
        };

        var test12_2 = new TestCase
        {
            ProgrammingTaskItem = task12,
            InputJson = "{ \"coins\": [2], \"amount\": 3 }",
            ExpectedJson = "-1",
            IsVisible = true
        };

        var test12_3 = new TestCase
        {
            ProgrammingTaskItem = task12,
            InputJson = "{ \"coins\": [1], \"amount\": 0 }",
            ExpectedJson = "0",
            IsVisible = false
        };

        // --- TAG RELATIONS FOR TASKS ---
        task1.Tags = new List<Tag> { tagCSharp, tagIntro };
        task2.Tags = new List<Tag> { tagCSharp, tagIntro };
        task3.Tags = new List<Tag> { tagAlgo, tagStrings };
        task4.Tags = new List<Tag> { tagArrays, tagIntro };
        task5.Tags = new List<Tag> { tagStack, tagStrings };
        task6.Tags = new List<Tag> { tagArrays, tagSorting };
        task7.Tags = new List<Tag> { tagTrees };
        task8.Tags = new List<Tag> { tagStrings, tagDP };
        task9.Tags = new List<Tag> { tagStack, tagQueue };
        task10.Tags = new List<Tag> { tagArrays, tagAlgo };
        task11.Tags = new List<Tag> { tagGraphs, tagAlgo };
        task12.Tags = new List<Tag> { tagDP, tagAlgo };
        task13.Tags = new List<Tag> { tagIntro, tagCSharp };
        task14.Tags = new List<Tag> { tagIntro, tagArrays };

        // --- COURSE-TASK RELATIONS ---
        course1.TaskItems = new List<TaskItem> { task1, task2, task4, task5, task6, task13, task14 };
        course2.TaskItems = new List<TaskItem> { task3, task7, task9 };
        course3.TaskItems = new List<TaskItem> { task8, task10, task11, task12 };

        // --- LECTURE-TAG RELATIONSHIPS ---
        // Course 1 - C# Programming Fundamentals
        lec1_1.Tags = new List<Tag> { tagIntro, tagCSharp };
        lec1_2.Tags = new List<Tag> { tagCSharp };
        lec1_3.Tags = new List<Tag> { tagCSharp };
        lec1_4.Tags = new List<Tag> { tagCSharp };
        lec1_5.Tags = new List<Tag> { tagArrays, tagIntro };

        // Course 2 - Data Structures Essentials
        lec2_1.Tags = new List<Tag> { tagArrays, tagData };
        lec2_2.Tags = new List<Tag> { tagArrays, tagAlgo };
        lec2_3.Tags = new List<Tag> { tagStack, tagData };
        lec2_4.Tags = new List<Tag> { tagQueue, tagData };
        lec2_5.Tags = new List<Tag> { tagLinkedList, tagData };
        lec2_6.Tags = new List<Tag> { tagTrees, tagData };

        // Course 3 - Advanced Algorithms
        lec3_1.Tags = new List<Tag> { tagGraphs, tagAlgo };
        lec3_2.Tags = new List<Tag> { tagDP, tagAlgo };
        lec3_3.Tags = new List<Tag> { tagSorting, tagAlgo };
        lec3_4.Tags = new List<Tag> { tagAlgo };

        // --- SAVE ALL ---
        await context.Courses.AddRangeAsync(course1, course2, course3);
        await context.Lectures.AddRangeAsync(
            lec1_1, lec1_2, lec1_3, lec1_4, lec1_5,
            lec2_1, lec2_2, lec2_3, lec2_4, lec2_5, lec2_6,
            lec3_1, lec3_2, lec3_3, lec3_4
        );
        await context.LectureContents.AddRangeAsync(
            content1_1, content1_2, content1_3, content1_5,
            content2_1, content2_2, content2_3, content2_6,
            content3_1, content3_2
        );
        await context.TaskItems.AddRangeAsync(
            task1, task2, task3, task4, task5, task6, task7, task8,
            task9, task10, task11, task12, task13, task14
        );
        await context.TestCases.AddRangeAsync(
            test1, test1_2, test1_3,
            test2, test2_2, test2_3,
            test3, test3_2, test3_3, test3_4,
            test4, test4_2, test4_3,
            test5, test5_2, test5_3, test5_4,
            test6, test6_2,
            test7, test7_2, test7_3,
            test8, test8_2, test8_3,
            test10, test10_2, test10_3,
            test11, test11_2, test11_3,
            test12, test12_2, test12_3
        );

        await context.SaveChangesAsync();
        Console.WriteLine($"Saved all entities. Courses count: {await context.Courses.CountAsync()}");

        // --- COURSE PROGRESS ---
        // John has progress in course1 (60%)
        var progress1 = new CourseProgress
        {
            UserId = users["john"].Id,
            CourseId = course1.Id,
            Percentage = 60,
            StartedAt = DateTime.UtcNow.AddDays(-30),
            CompletedAt = null
        };

        // Alice completed course1 (100%)
        var progress2 = new CourseProgress
        {
            UserId = users["alice"].Id,
            CourseId = course1.Id,
            Percentage = 100,
            StartedAt = DateTime.UtcNow.AddDays(-45),
            CompletedAt = DateTime.UtcNow.AddDays(-5)
        };

        // Alice started course2 (20%)
        var progress3 = new CourseProgress
        {
            UserId = users["alice"].Id,
            CourseId = course2.Id,
            Percentage = 20,
            StartedAt = DateTime.UtcNow.AddDays(-10),
            CompletedAt = null
        };

        // Mark has progress in course2 (45%)
        var progress4 = new CourseProgress
        {
            UserId = users["mark"].Id,
            CourseId = course2.Id,
            Percentage = 45,
            StartedAt = DateTime.UtcNow.AddDays(-20),
            CompletedAt = null
        };

        // John started course3 (15%)
        var progress5 = new CourseProgress
        {
            UserId = users["john"].Id,
            CourseId = course3.Id,
            Percentage = 15,
            StartedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = null
        };

        await context.CourseProgresses.AddRangeAsync(
            progress1, progress2, progress3, progress4, progress5
        );

        await context.SaveChangesAsync();
        Console.WriteLine("SeedContentAsync completed successfully!");
    }

    private static async Task SeedAchievements(ApplicationDbContext context)
    {
        if (await context.Achievements.AnyAsync())
            return;

        var achievements = new List<Achievement>
        {
            // Task completion achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "First Steps",
                Description = "Complete your first 5 programming tasks",
                IconPath = "/icons/achievements/first-steps.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 5 tasks",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteTasks,
                            TargetValue = 5
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Problem Solver",
                Description = "Complete 10 programming tasks",
                IconPath = "/icons/achievements/problem-solver.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 10 tasks",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteTasks,
                            TargetValue = 10
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Code Master",
                Description = "Complete 15 programming tasks",
                IconPath = "/icons/achievements/code-master.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 15 tasks",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteTasks,
                            TargetValue = 15
                        }
                    }
                }
            },

            // Lecture completion achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Eager Learner",
                Description = "Complete your first 5 lectures",
                IconPath = "/icons/achievements/eager-learner.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 5 lectures",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteLectures,
                            TargetValue = 5
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Knowledge Seeker",
                Description = "Complete 10 lectures",
                IconPath = "/icons/achievements/knowledge-seeker.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 10 lectures",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteLectures,
                            TargetValue = 10
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Scholar",
                Description = "Complete 15 lectures",
                IconPath = "/icons/achievements/scholar.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 15 lectures",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteLectures,
                            TargetValue = 15
                        }
                    }
                }
            },

            // Course completion achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Course Completer",
                Description = "Complete your first course",
                IconPath = "/icons/achievements/course-completer.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 1 course",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteCourses,
                            TargetValue = 1
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Dedicated Student",
                Description = "Complete 3 courses",
                IconPath = "/icons/achievements/dedicated-student.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 3 courses",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteCourses,
                            TargetValue = 3
                        }
                    }
                }
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Graduate",
                Description = "Complete 5 courses",
                IconPath = "/icons/achievements/graduate.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 5 courses",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteCourses,
                            TargetValue = 5
                        }
                    }
                }
            },

            // Combined achievement
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Well Rounded",
                Description = "Complete 10 tasks, 10 lectures, and 2 courses",
                IconPath = "/icons/achievements/well-rounded.png",
                Requirements = new List<Requirement>
                {
                    new Requirement
                    {
                        Description = "Complete 10 tasks",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteTasks,
                            TargetValue = 10
                        }
                    },
                    new Requirement
                    {
                        Description = "Complete 10 lectures",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteLectures,
                            TargetValue = 10
                        }
                    },
                    new Requirement
                    {
                        Description = "Complete 2 courses",
                        Condition = new RequirementCondition
                        {
                            Type = RequirementType.CompleteCourses,
                            TargetValue = 2
                        }
                    }
                }
            }
        };

        await context.Achievements.AddRangeAsync(achievements);
        await context.SaveChangesAsync();
    }
}






