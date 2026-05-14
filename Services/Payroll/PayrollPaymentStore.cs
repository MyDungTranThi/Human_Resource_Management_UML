using System.Collections.Concurrent;

namespace HRM.Web.Services.Payroll;

public class PayrollPaymentStore
{
    private readonly ConcurrentDictionary<string, bool> _paidMap = new();

    public void MarkPaid(int empId, int month, int year)
    {
        _paidMap[BuildKey(empId, month, year)] = true;
    }

    public bool IsPaid(int empId, int month, int year)
    {
        return _paidMap.ContainsKey(BuildKey(empId, month, year));
    }

    private static string BuildKey(int empId, int month, int year)
    {
        return $"{empId}:{month}:{year}";
    }
}
