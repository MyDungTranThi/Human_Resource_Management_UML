using HRM.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Web.Scratch;

public class DbInvestigator
{
    public static async Task ListAllPositions(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HRMDbContext>();
        
        var positions = await context.Positions
            .Select(p => p.PosName)
            .ToListAsync();
            
        Console.WriteLine("--- DANH SACH CHUC DANH TRONG DATABASE ---");
        foreach (var pos in positions)
        {
            Console.WriteLine($"- '{pos}'");
        }
        Console.WriteLine("------------------------------------------");
    }
}
