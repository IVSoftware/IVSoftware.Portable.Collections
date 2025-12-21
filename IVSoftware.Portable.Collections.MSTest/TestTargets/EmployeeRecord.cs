using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    class EmployeeRecord
    {
        // Constructors
        public EmployeeRecord() { }

        public EmployeeRecord(int id, string name)
        {
            Id = id;
            Name = name;
            HireDate = DateTime.Now;
            IsActive = true;
        }

        // Properties
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }

        // Events
        public event EventHandler? Promoted;
        public event EventHandler? Terminated;

        // Methods (intentionally overloaded for duplicate-name testing)
        public void Promote(string newDepartment)
        {
            Department = newDepartment;
            Promoted?.Invoke(this, EventArgs.Empty);
        }

        public void Promote(string newDepartment, decimal newSalary)
        {
            Department = newDepartment;
            Salary = newSalary;
            Promoted?.Invoke(this, EventArgs.Empty);
        }

        public void Promote(string newDepartment, string newTitle, decimal newSalary)
        {
            Department = newDepartment;
            Title = newTitle;
            Salary = newSalary;
            Promoted?.Invoke(this, EventArgs.Empty);
        }

        public void Terminate()
        {
            IsActive = false;
            Terminated?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return $"{Id}: {Name} ({Title}, {Department})";
        }
    }

}
