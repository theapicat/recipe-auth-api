using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;

namespace Tests.Support;

/// <summary>
/// Per-test testrigg som setter opp en EKTE UserManager/SignInManager/RoleManager mot en
/// SQLite in-memory-database, i stedet for å mocke Identity med NSubstitute.
///
/// Se Documentation/06-test-strategy.md pkt. 3 for begrunnelsen: UserManager/SignInManager
/// har for mye intern logikk (passordhashing, lockout-telling, concurrency stamps) til at
/// det er trygt å re-implementere den atferden via mocks.
///
/// Opprett én ny instans PER TEST (test-klassens konstruktør, ikke en delt IClassFixture)
/// slik at hver test garantert får en tom, uavhengig database. SQLite in-memory + EnsureCreated
/// er raskt nok (millisekunder) til at dette ikke går på bekostning av testhastighet.
/// </summary>
public sealed class HandlerTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public ApplicationDbContext DbContext { get; }
    public UserManager<ApplicationUser> UserManager { get; }
    public SignInManager<ApplicationUser> SignInManager { get; }
    public RoleManager<IdentityRole<Guid>> RoleManager { get; }

    public HandlerTestHarness()
    {
        // "DataSource=:memory:" + en åpen tilkobling som holdes i live for hele testens
        // levetid er det som gir en SQLite in-memory-database i stedet for en fil.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(_connection);
            options.UseOpenIddict<Guid>();
        });

        services.AddLogging();

        // SignInManager.Context kaster hvis HttpContext er null, selv om metodene vi
        // bruker i praksis (CheckPasswordSignInAsync, CanSignInAsync, IsLockedOutAsync)
        // ikke selv leser cookies. En tom DefaultHttpContext holder den i live.
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Speiler API/Extensions/OpenIddictExtensions.cs sine passordregler
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DbContext.Database.EnsureCreated();

        UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        SignInManager = _scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        RoleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Rollene handlerne forventer å finne (speiler IdentitySeeder)
        RoleManager.CreateAsync(new IdentityRole<Guid>("Admin")).GetAwaiter().GetResult();
        RoleManager.CreateAsync(new IdentityRole<Guid>("User")).GetAwaiter().GetResult();
    }

    /// <summary>Oppretter en testbruker med passord og gitt rolle. Standard: bekreftet e-post, vanlig bruker.</summary>
    public async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password = "TestPass123!",
        bool isAdmin = false,
        bool emailConfirmed = true)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "Bruker",
            EmailConfirmed = emailConfirmed,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Kunne ikke opprette testbruker: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await UserManager.AddToRoleAsync(user, isAdmin ? "Admin" : "User");
        return user;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}
