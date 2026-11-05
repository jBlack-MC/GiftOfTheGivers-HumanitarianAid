using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
[IgnoreAntiforgeryToken]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AuthApiController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Authenticates user credentials for mobile/API clients.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthApiResponse>> Login([FromBody] LoginApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthApiResponse { Success = false, Message = "Email and Password are required." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized(new AuthApiResponse { Success = false, Message = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return StatusCode(StatusCodes.Status423Locked, new AuthApiResponse { Success = false, Message = "Account locked out due to multiple failed login attempts. Please try again later." });
            }
            return Unauthorized(new AuthApiResponse { Success = false, Message = "Invalid email or password." });
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        var roles = (await _userManager.GetRolesAsync(user)).ToList();

        return Ok(new AuthApiResponse
        {
            Success = true,
            Message = "Login successful.",
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        });
    }

    /// <summary>
    /// Registers a new donor account.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthApiResponse>> Register([FromBody] RegisterApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthApiResponse { Success = false, Message = "Email and Password are required." });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new AuthApiResponse { Success = false, Message = "Passwords do not match." });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser is not null)
        {
            return BadRequest(new AuthApiResponse { Success = false, Message = "An account with this email already exists." });
        }

        var user = new AppUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return BadRequest(new AuthApiResponse { Success = false, Message = errors });
        }

        await _userManager.AddToRoleAsync(user, ClaimsPrincipalExtensions.DonorRole);
        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new AuthApiResponse
        {
            Success = true,
            Message = "Account created successfully.",
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = new List<string> { ClaimsPrincipalExtensions.DonorRole }
        });
    }

    /// <summary>
    /// Returns details for the currently signed-in user.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<AuthApiResponse>> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new AuthApiResponse { Success = false, Message = "User is not authenticated." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new AuthApiResponse { Success = false, Message = "User session invalid." });
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();

        return Ok(new AuthApiResponse
        {
            Success = true,
            Message = "Authenticated user retrieved.",
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        });
    }

    /// <summary>
    /// Signs out the user session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { success = true, message = "Successfully signed out." });
    }
}
