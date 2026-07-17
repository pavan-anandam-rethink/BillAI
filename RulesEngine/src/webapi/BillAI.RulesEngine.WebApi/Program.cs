using BillAI.RulesEngine.Application.DependencyInjection;
using BillAI.RulesEngine.Engine.DependencyInjection;
using BillAI.RulesEngine.Infrastructure.DependencyInjection;
using BillAI.WorkflowEngine.Engine.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Application (MediatR + FluentValidation)
builder.Services.AddApplicationServices();

// Infrastructure (storage providers + repositories + tenant service)
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Rules runtime engine
builder.Services.AddRuleEngine();

// Workflow execution engine
builder.Services.AddWorkflowEngine();

// JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BillAI-RulesEngine-Dev-Secret-Key-32chars!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BillAI Rules Engine API",
        Version = "v1",
        Description = "Healthcare Claims Validation & Workflow Orchestration Platform"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ─── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
