using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DMS.Data.EF.Models;
using DMS.API.Model;
using DMS.Application.Interface;

namespace DMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _service;
        private readonly IConfiguration _configuration;
        public LoginController(IConfiguration configuration, ILoginService service)
        {
            _configuration = configuration; 
            _service = service;
        }
        
        [HttpGet("dbinfo")]
        public IActionResult GetDbInfo()
        {
            var (server, database) = _service.GetDbInfo();
            return Ok(new { server, database });
        }

        [HttpPost]
        public ActionResult Login([FromBody] UserLogin userLogin)
        {
            var user = AuthenticateUser(userLogin);
            if (user != null)
            {
                var token = GenerateToken(user);
                return Ok(token);
            }
            return NotFound("User not found");
        }

        // To generate token
        private string GenerateToken(UserModel user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.UserName),
                new Claim(ClaimTypes.Role,user.Role)
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials);


            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        // Marking the AuthenticateUser method as static since it does not access instance data.
        private static UserModel? AuthenticateUser(UserLogin userLogin)
        {
            var currentUser = UserConstants.Users.FirstOrDefault(x => x.UserName.ToLower() ==
                userLogin.UserName.ToLower() && x.Password == userLogin.Password);

            return currentUser;
        }
    }
}
