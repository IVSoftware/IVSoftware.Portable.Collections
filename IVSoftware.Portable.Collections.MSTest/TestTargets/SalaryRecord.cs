using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    class SalaryRecord
    {
        // Properties
        public int EmployeeId { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Currency { get; set; } = "USD";

        // Events
        public event EventHandler? SalaryChanged;
        public event EventHandler? BonusApplied;

        // Constructors
        public SalaryRecord() { }

        public SalaryRecord(int employeeId, decimal baseSalary)
        {
            EmployeeId = employeeId;
            BaseSalary = baseSalary;
            EffectiveDate = DateTime.Today;
        }

        // Methods
        public void ApplyRaise(decimal percent)
        {
            if (percent <= 0) return;
            BaseSalary += BaseSalary * percent / 100m;
            SalaryChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyBonus(decimal amount)
        {
            Bonus += amount;
            BonusApplied?.Invoke(this, EventArgs.Empty);
        }

        public decimal TotalCompensation()
        {
            return BaseSalary + Bonus;
        }

        public override string ToString()
        {
            return $"{EmployeeId}: {Currency} {(BaseSalary + Bonus):N2}";
        }
    }

}
