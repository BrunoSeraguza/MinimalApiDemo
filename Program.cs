using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalApiDemo.Data;
using MinimalApiDemo.Models;
using MiniValidation;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

namespace MinimalApiDemo;

public class Program
{
    public static   void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();


        builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Minimal API Sample",
                Description = "Developed by Bruno Seraguza - Owner @ https://www.linkedin.com/in/bruno-seraguza/",
                Contact = new OpenApiContact { Name = "Bruno Seraguza", Email = "shzdevops@gmail.com" },
                License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
            });

             c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
             {
                Description = "Insira o token JWT desta maneira: Bearer {seu token}",
                Name = "Authorization",
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
             });

             c.AddSecurityRequirement(new OpenApiSecurityRequirement
             {
                {
                   new OpenApiSecurityScheme
                   {
                        Reference = new OpenApiReference
                        {
                               Type = ReferenceType.SecurityScheme,
                               Id = "Bearer"
                        }
                   },
                            new string[] {}
                }
             });
        });

        builder.Services.AddDbContext<MinimalApiContextDb>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MinimalApiDemo")));
        builder.Services.AddIdentityConfiguration();

        builder.Services.AddJwtConfiguration(builder.Configuration  , "AppSettings");

        builder.Services.AddAuthorization( options =>
        {
            options.AddPolicy("ExcluirFornecedor", policy => 
            policy.RequireClaim("ExcluirFornecedor"));

        }) ;

        var app = builder.Build();


        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

     

        app.UseAuthConfiguration();
        app.UseHttpsRedirection();
        //Registro
        app.MapPost("/registro", [AllowAnonymous] async (
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IOptions<AppJwtSettings> options,
            RegisterUser registerUser
            ) =>
           {
               if (registerUser == null) return Results.BadRequest("Usuario não encontrado");

               if (!MiniValidator.TryValidate(registerUser, out var error)) return Results.ValidationProblem(error);

               var user = new IdentityUser
               {
                   UserName = registerUser.Email,
                   Email = registerUser.Email,
                   EmailConfirmed = true
               };
               var result = await userManager.CreateAsync(user, registerUser.Password);

               if (!result.Succeeded) return Results.BadRequest(result.Errors);

               var jwt = new JwtBuilder()
               .WithUserManager(userManager)
               .WithJwtSettings(options.Value)
               .WithEmail(user.Email)
               .WithJwtClaims()
               .WithUserClaims()
               .WithUserRoles()
               .BuildUserResponse();

               return Results.Ok(jwt);

           }).ProducesValidationProblem()
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .WithName("RegistroUsuario")
           .WithTags("Usuario");

        //Login
        app.MapPost("/login", [AllowAnonymous] async (
            SignInManager < IdentityUser > signInManager,
            UserManager < IdentityUser > userManager,
            IOptions < AppJwtSettings > appJwtSettings,
             LoginUser loginUser) =>
        {
            if (loginUser == null) return Results.BadRequest("Usuario nao informado");

            if (!MiniValidator.TryValidate(loginUser, out var error))            
                return   Results.ValidationProblem(error);
            

            var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.IsLockedOut) Results.BadRequest("Usuario bloqueado");

            if (!result.Succeeded) return Results.BadRequest("Usuario ou senha invalidos");

            var jwt = new JwtBuilder()
            .WithUserManager(userManager)
               .WithJwtSettings(appJwtSettings.Value)
               .WithEmail(loginUser.Email)
               .WithJwtClaims()
               .WithUserClaims()              
               .WithUserRoles()
               .BuildUserResponse();

            return Results.Ok(jwt); 

        }).ProducesValidationProblem()
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .WithName("RegistroLogin")
           .WithTags("Usuario");

        //GetFornecedores
        app.MapGet("/fornecedores", [AllowAnonymous] async (MinimalApiContextDb context) =>
            await context.Fornecedor.ToListAsync())
            .WithName("GetFornecedores")
            .WithTags("Fornecedor");

        //Get por id
        app.MapGet("/fornecedores/{id}", async (MinimalApiContextDb context, Guid id) =>
        await context.Fornecedor.FindAsync(id)
        is Fornecedor fornecedor
        ? Results.Ok(fornecedor)
        : Results.NotFound())
        .Produces<Fornecedor>(StatusCodes.Status200OK)
        .Produces<Fornecedor>(StatusCodes.Status404NotFound)
        .WithName("GetFornecedoresWithId")
        .WithTags("Fornecedor");

        //add fornecedor
        app.MapPost("/fornecedores", [Authorize] async (MinimalApiContextDb context, Fornecedor fornecedor) =>
        {
            context.Fornecedor.Add(fornecedor);
            var result = await context.SaveChangesAsync();

            if (!MiniValidator.TryValidate(fornecedor, out var error))
                return Results.ValidationProblem(error);


            if (result > 0)
            {
                return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
            }
            else
            {
                return Results.BadRequest("Houve um erro ao adicionar um fornecedor");
            }
            // pode ser com ternario ou com if, anyway
            //return result > 0
            //? //Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor) 
            //Results.CreatedAtRoute($"/fornecedores{fornecedor.Id}", new { id = fornecedor.Id, fornecedor })
            //: Results.BadRequest("Houve um erro ao adicionar um fornecedor");


        }).Produces<Fornecedor>(StatusCodes.Status201Created)
        .Produces<Fornecedor>(StatusCodes.Status404NotFound)
        .WithName("PostFornecedores")
        .WithTags("Fornecedor");

        //alterar fornecedor
        app.MapPut("/fornecedores/{id}", async (Guid id, MinimalApiContextDb context, Fornecedor fornecedor) =>
        {
            //var fornecedorBanco = await context.Fornecedor.FindAsync(id); erro 500 ¬¬
            var fornecedorBanco = await context.Fornecedor.AsNoTracking<Fornecedor>().FirstOrDefaultAsync(f => f.Id == id);
            if (fornecedorBanco == null) return Results.NotFound();

            if (!MiniValidator.TryValidate(fornecedor, out var error))
            {
                return Results.ValidationProblem(error);
            }

            context.Fornecedor.Update(fornecedor);
            var result = await context.SaveChangesAsync();

            return result > 0
            ? Results.NotFound()
            : Results.BadRequest("Nao foi possivel editar o fornecedor");
        }).Produces<Fornecedor>(StatusCodes.Status204NoContent)
        .Produces<Fornecedor>(StatusCodes.Status400BadRequest)
        .WithName("PutFornecedores")
        .WithTags("Fornecedor");

     
        //deletar fornecedor 
        app.MapDelete("/fornecedor/{id}", [Authorize] async (MinimalApiContextDb context, Guid id) =>
        {
            var fornecedor = await context.Fornecedor.FindAsync(id);
            if (fornecedor == null) return Results.NotFound();

            context.Fornecedor.Remove(fornecedor);
            var result = await context.SaveChangesAsync();

            return result > 0
            ? Results.NoContent()
            : Results.BadRequest();

        }
        ).Produces<Fornecedor>(StatusCodes.Status204NoContent)
        .Produces<Fornecedor>(StatusCodes.Status400BadRequest)
        .Produces<Fornecedor>(StatusCodes.Status404NotFound)
        .RequireAuthorization("ExcluirFornecedor")
        .WithName("DeleteFornecedores")
        .WithTags("Fornecedor");




        app.UseAuthorization();


        app.Run();
    }
}