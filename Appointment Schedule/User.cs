using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer;
using Microsoft.Extensions.Configuration.Json;

namespace Appointment_Schedule
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string UserAddress { get; set; } = null!;
        public int UserPhone { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public class Appointment
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string AppointmentTitle { get; set; } = null!;
        public string AppointmentDescription { get; set; } = null!;
        public string AppointmentAddress { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; }
    }


    public class AppointmentSchedulerContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile(@"C:\Users\user\OneDrive\Desktop\FinalProject\Appointment Schedule\Appointment Schedule\Config.json")
                .Build();
            String? connectionString = configuration.GetConnectionString("ServerConnection");
            optionsBuilder.UseSqlServer(connectionString);
            base.OnConfiguring(optionsBuilder);

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Appointment>().HasKey(a => a.AppointmentId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Appointments)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId);
        }
    }
    public class Services
    {
        private AppointmentSchedulerContext _context;
        public Services(AppointmentSchedulerContext Context)
        {
            _context = Context;
        }

        public User Login(string username, string password)
        {
            return _context.Users.FirstOrDefault(u => u.UserName == username && u.Password == password);
        }

        public void Register(string username, string Password)
        {
            if (_context.Users.Any(u => u.UserName == username))
            {
                throw new Exception("User already exists");
            }
            var user = new User { UserName = username, Password = Password };
            _context.Users.Add(user);
            _context.SaveChanges();
        }
    }

    public class AppointmentRules
    {
        private AppointmentSchedulerContext _context;
        public AppointmentRules(AppointmentSchedulerContext Context)
        {
            _context = Context;
        }
        public void AddAppointment(int UserId, DateTime Appointmentdate, DateTime AppointmentTime, string AppointmentTitle, String AppointmentDescription, String AppointmentAddress)
        {
            var appointment = new Appointment
            {
                AppointmentDate = Appointmentdate,
                AppointmentTitle = AppointmentTitle,
                AppointmentDescription = AppointmentDescription,
                AppointmentAddress = AppointmentAddress,
                UserId = UserId,
                AppointmentTime = AppointmentTime
            };

            if (IsConflict(UserId, Appointmentdate, AppointmentTime))
            {
                throw new Exception("Appointment detected");
            }

            _context.Appointments.Add(appointment);
            _context.SaveChanges();
        }

        public List<Appointment> GetAppointments(int userId)
        {
            return _context.Appointments.Where(a => a.UserId == userId).ToList();
        }

        public void UpdateAppointment(int AppointmentId, DateTime AppointmentDate, DateTime AppointmentTime, string AppointmentTitle, string AppointmentDescription, string AppointmentAddress)
        {
            var appointment = _context.Appointments.Find(AppointmentId);
            if (appointment == null) throw new Exception("Appointment is not exist");

            if (IsConflict(appointment.UserId, AppointmentDate, AppointmentTime))
            {
                throw new Exception("Appointment detected");
            }
            appointment.AppointmentDate = AppointmentDate;
            appointment.AppointmentTime = AppointmentTime;
            appointment.AppointmentTitle = AppointmentTitle;
            appointment.AppointmentDescription = AppointmentDescription;
            appointment.AppointmentAddress = AppointmentAddress;
            _context.SaveChanges();
        }
        public void DeleteAppointment(int AppointmentId)
        {
            var appointment = _context.Appointments.Find(AppointmentId);
            if (appointment == null) throw new Exception("Appointment is not exist");
            _context.Appointments.Remove(appointment);
            _context.SaveChanges();
        }
        public List<Appointment> SearchAppointments(int userId, string title)
        {
            return _context.Appointments
                .Where(a => a.UserId == userId && a.AppointmentTitle.Contains(title))
                .ToList();
        }
        private bool IsConflict(int userId, DateTime date, DateTime time)
        {
            return _context.Appointments
                .Any(a => a.UserId == userId && a.AppointmentDate == date && a.AppointmentTime == time);

        }


        class Program
        {
            static void Main(string[] args)
            {
                using (var context = new AppointmentSchedulerContext())
                {
                    var authService = new Services(context);
                    var appointmentService = new AppointmentRules(context);

                    Console.WriteLine("Welcome to the Appointment Scheduler!");

                    User currentUser = null;
                    while (currentUser == null)
                    {
                        Console.WriteLine("1. Login");
                        Console.WriteLine("2. Register");
                        var choice = Console.ReadLine();

                        Console.Write("Please Enter Your UserName: ");
                        var username = Console.ReadLine();
                        Console.Write("Your Password: ");
                        var Password = Console.ReadLine();


                        if (choice == "1")
                        {
                            currentUser = authService.Login(username, Password);
                            if (currentUser == null)
                            {
                                Console.WriteLine("there is error.. Please Try Again");
                            }
                        }
                        else if (choice == "2")
                        {
                            try
                            {
                                authService.Register(username, Password);
                                Console.WriteLine("Registration is done.. can you login");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                        }
                    }

                    while (true)
                    {
                        Console.WriteLine("1. Add Appointment");
                        Console.WriteLine("2. View Appointments");
                        Console.WriteLine("3. Update Appointment");
                        Console.WriteLine("4. Delete Appointment");
                        Console.WriteLine("5. Search Appointments");
                        Console.WriteLine("6. Exit");

                        var choice = Console.ReadLine();

                        switch (choice)
                        {
                            case "1":
                                Console.Write("Date: ");
                                var AppointmentDate = DateTime.Parse(Console.ReadLine());
                                Console.Write("Time: ");
                                var AppointmentTime = DateTime.Parse(Console.ReadLine());
                                Console.Write("Title: ");
                                var AppointmentTitle = Console.ReadLine();
                                Console.Write("description: ");
                                var AppointmentDescription = Console.ReadLine();
                                Console.Write("Address: ");
                                var AppointmentAddress = Console.ReadLine();

                                try
                                {
                                    appointmentService.AddAppointment(currentUser.UserId, AppointmentDate, AppointmentTime, AppointmentTitle, AppointmentDescription, AppointmentAddress);
                                    Console.WriteLine("Appointment added");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                                break;

                            case "2":
                                var appointments = appointmentService.GetAppointments(currentUser.UserId);
                                foreach (var app in appointments)
                                {
                                    Console.WriteLine($"{app.AppointmentId}:{app.AppointmentDate.ToShortDateString()} {app.AppointmentTime}-{app.AppointmentTitle} at {app.AppointmentAddress}");
                                }
                                break;
                            case "3":
                                Console.Write("Update AppointmentId: ");
                                var updateAppointmentId = int.Parse(Console.ReadLine());
                                Console.Write("New date: (dd-mm-yyyy) ");
                                var newAppointmentDate = DateTime.Parse(Console.ReadLine());
                                Console.Write("New time: (hh:mm) ");
                                var newAppointmentTime = DateTime.Parse(Console.ReadLine());
                                Console.Write("new Title: ");
                                var newAppointmentTitle = Console.ReadLine();
                                Console.Write("new Description: ");
                                var newAppointmentDescription = Console.ReadLine();
                                Console.Write("new Address: ");
                                var newAppointmentAddress = Console.ReadLine();

                                try
                                {
                                    appointmentService.UpdateAppointment(updateAppointmentId, newAppointmentDate, newAppointmentTime, newAppointmentTitle, newAppointmentDescription, newAppointmentAddress);
                                    Console.WriteLine("Appointment updated");
                                }

                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }

                                break;



                            case "4":
                                Console.Write("Appointment id to delete: ");
                                var deletedId = int.Parse(Console.ReadLine());

                                try
                                {
                                    appointmentService.DeleteAppointment(deletedId);
                                    Console.WriteLine("Appointment deleted");
                                }
                                catch (Exception ex)
                                {
                                    Console.Write(ex.Message);
                                }

                                break;



                            case "5":
                                Console.Write("Search by title: ");
                                var searchTitle = Console.ReadLine();
                                var SearchResult = appointmentService.SearchAppointments(currentUser.UserId, searchTitle);
                                foreach (var app in SearchResult)
                                {
                                    Console.WriteLine($"{app.AppointmentId}: {app.AppointmentDate.ToShortDateString()} {app.AppointmentTime} - {app.AppointmentTitle} at {app.AppointmentAddress}");

                                }

                                break;


                            case "6":
                                currentUser = null;
                                Console.WriteLine("exit");
                                break;
                                    }
                    }
                }
            }
        }
    }
}

