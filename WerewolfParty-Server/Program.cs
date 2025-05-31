using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WerewolfParty_Server.API;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Mappers;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Repository;
using WerewolfParty_Server.Role;
using WerewolfParty_Server.Service;
using WerewolfParty_Server.Validator;

namespace WerewolfParty_Server;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins") ?? "";


        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "WerewolfServerPolicy",
                policy =>
                {
                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                    policy.WithOrigins(allowedOrigins);
                    policy.AllowCredentials();
                });
        });


        var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("ConnectionString is missing.");
        }
        builder.Services.AddDbContext<WerewolfDbContext>(opt => opt.UseNpgsql(connectionString));
        
        builder.Services.AddScoped<RoomRepository>();
        builder.Services.AddScoped<PlayerRoomRepository>();
        builder.Services.AddScoped<RoleSettingsRepository>();
        builder.Services.AddScoped<RoomGameActionRepository>();
        builder.Services.AddScoped<PlayerRoleRepository>();


        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<RoomService>();
        builder.Services.AddScoped<GameService>();
        builder.Services.AddScoped<RoleFactory>();
        builder.Services.AddSignalR((options) => { options.EnableDetailedErrors = true; });
        builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

        //Mappers
        builder.Services.AddAutoMapper(typeof(PlayerMapper));
        builder.Services.AddAutoMapper(typeof(PlayerRoleMapper));
        builder.Services.AddAutoMapper(typeof(PlayerQueuedActionMapper));
        builder.Services.AddAutoMapper(typeof(PlayerGameActionMapper));
        builder.Services.AddAutoMapper(typeof(RoomSettingsMapper));


        //Validators
        builder.Services.AddScoped<IValidator<PlayerDTO>, PlayerDTOValidator>();
        builder.Services.AddScoped<IValidator<UpdateRoleSettingsRequest>, RoleSettingsRequestValidator>();


        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //Auth
        var privateKeyValue = builder.Configuration.GetValue<string>("Auth:PrivateKey");
        var authIssuerValue = builder.Configuration.GetValue<string>("Auth:Issuer");
        var authAudienceValue = builder.Configuration.GetValue<string>("Auth:Audience");
        if (string.IsNullOrEmpty(privateKeyValue) || string.IsNullOrEmpty(authIssuerValue) ||
            string.IsNullOrEmpty(authAudienceValue))
        {
            throw new Exception("Auth public key and/or private key are missing.");
        }

        builder.Services.AddAuthentication(options =>
        {
            // Identity made Cookie authentication the default.
            // However, we want JWT Bearer Auth to be the default.
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            // Configure the Authority to the expected value for
            // the authentication provider. This ensures the token
            // is appropriately validated.
            //options.Authority = "AuthorityURL"; // TODO: Update URL

            // We have to hook the OnMessageReceived event in order to
            // allow the JWT authentication handler to read the access
            // token from the query string when a WebSocket or 
            // Server-Sent Events request comes in.

            // Sending the access token in the query string is required when using WebSockets or ServerSentEvents
            // due to a limitation in Browser APIs. We restrict it to only calls to the
            // SignalR hub in this code.
            // See https://docs.microsoft.com/aspnet/core/signalr/security#access-token-logging
            // for more information about security considerations when using
            // the query string to transmit the access token.
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authIssuerValue,
                ValidAudience = authAudienceValue,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKeyValue))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    // If the request is for our hub...
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/Events"))
                        // Read the token out of the query string
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //TODO: Enable later
        //app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.RegisterRoomEndpoints();
        app.RegisterPlayerEndpoints();
        app.RegisterGameEndpoints();

        app.UseExceptionHandler(_ => { });

        app.UseCors("WerewolfServerPolicy");


        //Hub
        app.MapHub<EventsHub>("/Events");


        app.Run();
    }
}
