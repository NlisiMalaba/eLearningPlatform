using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Privacy;
using EduZim.Infrastructure.Persistence.Encryption;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Identity;

/// <summary>
/// Feature: elearning-app-zimbabwe — Identity correctness properties (2, 3, 4, 42, 43).
/// </summary>
public sealed class IdentityPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 2: Password Storage Never Stores Plaintext — Validates: Requirements 1.8
    [Property(MaxTest = 500)]
    public void Property2_stored_password_hash_never_equals_plaintext_and_salting_varies_by_user(NonWhiteSpaceString password)
    {
        var pwd = password.Get.Trim();
        if (pwd.Length < 8)
            pwd = pwd + "Padding1";

        var hasher = new PasswordHasher<ApplicationUser>();
        var user1 = new ApplicationUser { Id = Guid.NewGuid() };
        var user2 = new ApplicationUser { Id = Guid.NewGuid() };

        var hash1 = hasher.HashPassword(user1, pwd);
        var hash2 = hasher.HashPassword(user2, pwd);

        Assert.NotEqual(pwd, hash1);
        Assert.NotEqual(pwd, hash2);
        Assert.NotEqual(hash1, hash2);
    }

    // Feature: elearning-app-zimbabwe, Property 3: Account Lockout Threshold — Validates: Requirements 1.5
    [Property(MaxTest = 100)]
    public async Task Property3_exactly_five_failed_attempts_locks_account_fewer_does_not()
    {
        using var provider = IdentityPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"lock-{Guid.NewGuid():N}@test.local";
        var password = "ValidPass1!";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = Guid.NewGuid(),
            Role = UserRole.Student,
        };

        AssertCreate(await users.CreateAsync(user, password).ConfigureAwait(false));

        for (var i = 0; i < 4; i++)
        {
            AssertIdentity(await users.AccessFailedAsync(user).ConfigureAwait(false));
            user = (await users.FindByIdAsync(user.Id.ToString()).ConfigureAwait(false))!;
            Assert.False(await users.IsLockedOutAsync(user).ConfigureAwait(false));
        }

        AssertIdentity(await users.AccessFailedAsync(user).ConfigureAwait(false));
        user = (await users.FindByIdAsync(user.Id.ToString()).ConfigureAwait(false))!;
        Assert.True(await users.IsLockedOutAsync(user).ConfigureAwait(false));
    }

    // Feature: elearning-app-zimbabwe, Property 4: Unverified Accounts Cannot Access Protected Resources — Validates: Requirements 1.2
    [Property(MaxTest = 100)]
    public async Task Property4_unverified_user_correct_password_sign_in_not_allowed()
    {
        using var provider = IdentityPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var email = $"unverified-{Guid.NewGuid():N}@test.local";
        var password = "ValidPass1!";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            TenantId = Guid.NewGuid(),
            Role = UserRole.Student,
        };

        AssertCreate(await users.CreateAsync(user, password).ConfigureAwait(false));
        user = (await users.FindByIdAsync(user.Id.ToString()).ConfigureAwait(false))!;

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true).ConfigureAwait(false);

        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAllowed);
    }

    // Feature: elearning-app-zimbabwe, Property 42: PII Encryption at Rest — Validates: Requirements 16.2
    [Property(MaxTest = 500)]
    public void Property42_persisted_pii_columns_not_equal_plaintext(NonWhiteSpaceString email, NonWhiteSpaceString phone, NonWhiteSpaceString fullName)
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var dataProtection = sp.GetRequiredService<IDataProtectionProvider>();

        var converter = PiiStringConverter.Create(dataProtection);

        var e = email.Get.Trim();
        var p = phone.Get.Trim();
        var f = fullName.Get.Trim();

        if (e.Length == 0 || p.Length == 0 || f.Length == 0)
            return;

        Assert.NotEqual(e, converter.ConvertToProvider(e));
        Assert.NotEqual(p, converter.ConvertToProvider(p));
        Assert.NotEqual(f, converter.ConvertToProvider(f));
    }

    // Feature: elearning-app-zimbabwe, Property 43: PII Deletion on Request — Validates: Requirements 16.4
    [Property(MaxTest = 500)]
    public void Property43_redaction_clears_or_tombstones_pii(Guid userId, NonWhiteSpaceString email)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email.Get,
            Email = email.Get,
            PhoneNumber = "+263000000000",
            FullName = "Original Name",
        };

        SubjectPiiRedaction.Apply(user);

        Assert.True(SubjectPiiRedaction.MeetsDeletionRequirement(user));
    }

    private static void AssertIdentity(IdentityResult result)
    {
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static void AssertCreate(IdentityResult result) => AssertIdentity(result);
}
