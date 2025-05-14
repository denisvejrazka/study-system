using System;
using System.Collections.Generic;
using System.Linq;

// ------------------------ STRATEGY PATTERN ------------------------
interface IGradingStrategy
{
    double CalculateGrade(List<(double Grade, double Weight)> grades);
}

public class ArithmeticAverageStrategy : IGradingStrategy
{
    public double CalculateGrade(List<(double Grade, double Weight)> grades)
    {
        if (grades == null || grades.Count == 0)
            return 0;

        return grades.Average(g => g.Grade);
    }
}


class WeightedAverageStrategy : IGradingStrategy
{
    public double CalculateGrade(List<(double Grade, double Weight)> grades)
    {
        double totalWeight = grades.Sum(g => g.Weight);
        if (totalWeight == 0) return 0;
        return grades.Sum(g => g.Grade * g.Weight) / totalWeight;
    }
}

// ------------------------ OBSERVER PATTERN ------------------------
interface IObserver
{
    void Update(string message);
}

interface IObservable
{
    void AddObserver(IObserver observer);
    void RemoveObserver(IObserver observer);
    void NotifyObservers(string message);
}

// ------------------------ USER TYPES AND FACTORY ------------------------
abstract class User
{
    public string Name { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public abstract void DisplayRole();
}

class Student : User, IObserver
{
    public List<Course> Courses { get; set; } = new();
    public override void DisplayRole() => Console.WriteLine($"Student: {Name}");

    public void Update(string message)
    {
        Console.WriteLine($"[Notifikace pro studenta {Name}]: {message}");
    }
}

class Teacher : User
{
    public override void DisplayRole() => Console.WriteLine($"Učitel: {Name}");
}

class Administrator : User
{
    public override void DisplayRole() => Console.WriteLine($"Správce: {Name}");

    public void ViewAllUsers(StudySystem system)
    {
        Console.WriteLine("\n--- Všichni uživatelé v systému ---");
        foreach (var user in system.GetAllUsers())
        {
            Console.WriteLine($"- {user.GetType().Name}: {user.Name} ({user.Username})");
        }
    }
}


abstract class UserFactory
{
    public abstract User CreateUser(string name, string username, string password);
}

class StudentFactory : UserFactory
{
    public override User CreateUser(string name, string username, string password)
        => new Student { Name = name, Username = username, Password = password };
}

class TeacherFactory : UserFactory
{
    public override User CreateUser(string name, string username, string password)
        => new Teacher { Name = name, Username = username, Password = password };
}

class AdministratorFactory : UserFactory
{
    public override User CreateUser(string name, string username, string password)
        => new Administrator { Name = name, Username = username, Password = password };
}

// ------------------------ COURSE ------------------------
class Course : IObservable
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Teacher Teacher { get; set; }
    public IGradingStrategy GradingStrategy { get; set; } = new ArithmeticAverageStrategy();

    private List<IObserver> observers = new();
    private List<(Student Student, List<(double Grade, double Weight)> Grades)> grades = new();

    public void AddObserver(IObserver observer)
    {
        observers.Add(observer);
    }

    public void RemoveObserver(IObserver observer)
    {
        observers.Remove(observer);
    }

    public void NotifyObservers(string message)
    {
        foreach (var observer in observers)
            observer.Update(message);
    }

    public void RegisterStudent(Student student)
    {
        grades.Add((student, new List<(double, double)>()));
        AddObserver(student);
        student.Courses.Add(this);
        NotifyObservers($"Byl jste zapsán do kurzu {Name}.");
    }

    public void AddGrade(Student student, double grade, double weight = 1.0)
    {
        var record = grades.FirstOrDefault(g => g.Student == student);
        record.Grades.Add((grade, weight));
    }

    public double GetFinalGrade(Student student)
    {
        var record = grades.FirstOrDefault(g => g.Student == student);
        return GradingStrategy.CalculateGrade(record.Grades);
    }

    public List<Student> GetEnrolledStudents()
    {
        return grades.Select(g => g.Student).ToList();
    }

}

// ------------------------ STUDY SYSTEM ------------------------
class StudySystem
{
    private List<User> users = new();
    private List<Course> courses = new();


    public List<User> GetAllUsers() => users;

    public User RegisterUser(UserFactory factory, string name, string username, string password)
    {
        if (users.Any(u => u.Username == username))
        {
            Console.WriteLine("Uživatel již existuje.");
            return null;
        }

        var user = factory.CreateUser(name, username, password);
        users.Add(user);
        Console.WriteLine($"Zaregistrován: {user.Name} ({user.GetType().Name})");
        return user;
    }

    public User Login(string username, string password)
    {
        var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
        if (user == null) Console.WriteLine("Neplatné přihlašovací údaje.");
        return user;
    }

    public Course CreateCourse(string name, string description, Teacher teacher)
    {
        var course = new Course { Name = name, Description = description, Teacher = teacher };
        courses.Add(course);
        Console.WriteLine($"Kurz vytvořen: {name}");
        return course;
    }


    public void UpdateCourseDescription(Course course, string newDescription)
    {
        course.Description = newDescription;
        course.NotifyObservers($"Kurz {course.Name} byl aktualizován.");
    }

    public void ShowResults(Student student)
    {
        Console.WriteLine($"\nVýsledky studenta {student.Name}:");
        foreach (var course in student.Courses)
        {
            double final = course.GetFinalGrade(student);
            Console.WriteLine($"  {course.Name}: {final:F2}");
        }
    }

    public List<Course> GetCourses() => courses;
}

// ------------------------ MAIN PROGRAM ------------------------
class Program
{
    static void Main()
    {
        StudySystem system = new();
        bool running = true;
        User currentUser = null;

        while (running)
        {
            Console.WriteLine("\n--- Studijní Systém ---");
            Console.WriteLine("1. Registrace\n2. Přihlášení\n3. Konec");
            Console.Write("Zvolte možnost: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Jméno: "); string name = Console.ReadLine();
                    Console.Write("Uživatelské jméno: "); string username = Console.ReadLine();
                    Console.Write("Heslo: "); string password = Console.ReadLine();
                    Console.WriteLine("Typ účtu (student/teacher/admin): ");
                    string type = Console.ReadLine()?.ToLower();

                    UserFactory factory = type switch
                    {
                        "teacher" => new TeacherFactory(),
                        "admin" => new AdministratorFactory(),
                        _ => new StudentFactory()
                    };

                    system.RegisterUser(factory, name, username, password);
                    break;

                case "2":
                    Console.Write("Uživatelské jméno: "); string loginUser = Console.ReadLine();
                    Console.Write("Heslo: "); string loginPass = Console.ReadLine();
                    currentUser = system.Login(loginUser, loginPass);
                    if (currentUser != null)
                        UserMenu(system, currentUser);
                    break;

                case "3":
                    running = false;
                    break;
            }
        }
    }

    static void UserMenu(StudySystem system, User user)
    {
        bool active = true;
        while (active)
        {
            Console.WriteLine($"\n--- Menu ({user.Name}) ---");
            if (user is Student student)
            {
                Console.WriteLine("1. Přihlásit se na kurz\n2. Zobrazit výsledky\n3. Odhlásit se");
                Console.Write("Volba: ");
                switch (Console.ReadLine())
                {
                    case "1":
                        var courses = system.GetCourses();
                        for (int i = 0; i < courses.Count; i++)
                            Console.WriteLine($"{i + 1}. {courses[i].Name}");
                        Console.Write("Zvolte kurz: ");
                        if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= courses.Count)
                            courses[index - 1].RegisterStudent(student);
                        break;
                    case "2":
                        system.ShowResults(student);
                        break;
                    case "3":
                        active = false;
                        break;
                }
            }
            else if (user is Teacher teacher)
            {
                Console.WriteLine("1. Vytvořit kurz\n2. Upravit kurz\n3. Přidat známku studentovi\n4. Odhlásit se");
                Console.Write("Volba: ");
                switch (Console.ReadLine())
                {
                    case "1":
                        Console.Write("Název kurzu: "); string cname = Console.ReadLine();
                        Console.Write("Popis: "); string desc = Console.ReadLine();
                        system.CreateCourse(cname, desc, teacher);
                        break;
                    case "2":
                        var allCourses = system.GetCourses();
                        var ownedCourses = allCourses.Where(c => c.Teacher == teacher).ToList();
                        for (int i = 0; i < ownedCourses.Count; i++)
                            Console.WriteLine($"{i + 1}. {ownedCourses[i].Name}");
                        Console.Write("Zvolte kurz k úpravě: ");
                        if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= ownedCourses.Count)
                        {
                            Console.Write("Nový popis: ");
                            string newDesc = Console.ReadLine();
                            system.UpdateCourseDescription(ownedCourses[index - 1], newDesc);
                        }
                        break;
                    case "3":
                        var teacherCourses = system.GetCourses().Where(c => c.Teacher == teacher).ToList();
                        for (int i = 0; i < teacherCourses.Count; i++)
                            Console.WriteLine($"{i + 1}. {teacherCourses[i].Name}");

                        Console.Write("Zvolte kurz: ");
                        if (int.TryParse(Console.ReadLine(), out int cindex) && cindex > 0 && cindex <= teacherCourses.Count)
                        {
                            var course = teacherCourses[cindex - 1];
                            var enrolled = course.GetEnrolledStudents();

                            if (enrolled.Count == 0)
                            {
                                Console.WriteLine("V kurzu nejsou žádní studenti.");
                                break;
                            }

                            for (int i = 0; i < enrolled.Count; i++)
                                Console.WriteLine($"{i + 1}. {enrolled[i].Name}");

                            Console.Write("Zvolte studenta: ");
                            if (int.TryParse(Console.ReadLine(), out int sindex) && sindex > 0 && sindex <= enrolled.Count)
                            {
                                Console.Write("Zadejte známku: ");
                                double.TryParse(Console.ReadLine(), out double grade);
                                Console.Write("Zadejte váhu (např. 1): ");
                                double.TryParse(Console.ReadLine(), out double weight);
                                course.AddGrade(enrolled[sindex - 1], grade, weight);
                                Console.WriteLine("Známka přidána.");
                            }
                        }
                        break;
                    case "4":
                        active = false;
                        break;
                }
            }
            else if (user is Administrator admin)
            {
                Console.WriteLine("1. Zobrazit všechny uživatele\n2. Odhlásit se");
                Console.Write("Volba: ");
                switch (Console.ReadLine())
                {
                    case "1":
                        admin.ViewAllUsers(system);
                        break;
                    case "2":
                        active = false;
                        break;
                }
            }
        }
    }
}
