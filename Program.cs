using Microsoft.EntityFrameworkCore;
using MinimalApiDemo.Data;
using MinimalApiDemo.Models;
using MiniValidation;

namespace MinimalApiDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();


        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<MinimalApiContextDb>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        var app = builder.Build();


        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapGet("/fornecedores", async (MinimalApiContextDb context) =>
            await context.Fornecedor.ToListAsync())
            .WithName("GetFornecedores")
            .WithTags("Fornecedor");


        app.MapGet("/fornecedores{id}", async (MinimalApiContextDb context, Guid id) =>
        await context.Fornecedor.FindAsync(id)
        is Fornecedor fornecedor
        ? Results.Ok(fornecedor)
        : Results.NotFound())
        .Produces<Fornecedor>(StatusCodes.Status200OK)
        .Produces<Fornecedor>(StatusCodes.Status404NotFound)
        .WithName("GetFornecedoresWithId")
        .WithTags("Fornecedor");


        app.MapPost("/fornecedores", async (MinimalApiContextDb context, Fornecedor fornecedor) =>
        {
            context.Fornecedor.Add(fornecedor);
            var result = await context.SaveChangesAsync();

            if(!MiniValidator.TryValidate(fornecedor, out var error))
                return Results.ValidationProblem(error);



            if ( result > 0)
            {
               return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
            }
            else
            {
               return Results.BadRequest("Houve um erro ao adicionar um fornecedor");
            }
            
                
            //return result > 0
            //? //Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor) 
            //Results.CreatedAtRoute($"/fornecedores{fornecedor.Id}", new { id = fornecedor.Id, fornecedor })
            //: Results.BadRequest("Houve um erro ao adicionar um fornecedor");
             

        }).Produces<Fornecedor>(StatusCodes.Status201Created)
        .Produces<Fornecedor>(StatusCodes.Status404NotFound)
        .WithName("PostFornecedores")
        .WithTags("Fornecedor");








        app.UseAuthorization();


        app.Run();
    }
}