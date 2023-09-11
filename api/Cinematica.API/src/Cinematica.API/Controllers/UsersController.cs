﻿using Cinematica.API.Data;
using Cinematica.API.Models.User;
using Cinematica.API.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration APP_CONFIG;
        private DataContext _context;
        private AmazonCognitoIdentityProviderClient cognitoIdClient;

        public UsersController(IConfiguration config, DataContext context)
        {
            _context = context;

            APP_CONFIG = config.GetSection("AWS");

            cognitoIdClient = new AmazonCognitoIdentityProviderClient
            (
                APP_CONFIG["AccessKeyId"],
                APP_CONFIG["AccessSecretKey"],
                RegionEndpoint.GetBySystemName(APP_CONFIG["Region"])
            );
        }

        // GET api/Users/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var cognitoUser = await GetCognitoUser(id);
                var userId = cognitoUser.Attributes.ToArray()[0].Value;

                // get user from postgre database
                var databaseUser = _context.Users.SingleOrDefault(u => u.UserId.Equals(userId));

                // get follower and following count
                var follower_count = _context.UserFollowers.Count(u => u.UserId == userId);
                var following_count = _context.UserFollowers.Count(u => u.FollowerId == userId);

                return Ok(new { 
                    id = userId,
                    username = cognitoUser.Username,
                    profile_picture = databaseUser.ProfilePicture,
                    cover_picture = databaseUser.CoverPicture,
                    follower_count = follower_count ,
                    following_count = following_count,
                });
            }
            catch(UserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch(Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/follow
        [HttpPost("follow")]
        public IActionResult Follow([FromBody] FollowRequest model)
        {
            try
            {
                _context.Add(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId});
                _context.SaveChanges();
                return Ok(new { message = "Follow success." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/unfollow
        [HttpPost("unfollow")]
        public IActionResult Unfollow([FromBody] FollowRequest model)
        {
            try
            {
                _context.Remove(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId });
                _context.SaveChanges();
                return Ok(new { message = "Unfollow success." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/followers/id
        [HttpGet("followers/{id}")]
        public async Task<IActionResult> GetFollowers(string id)
        {
            try
            {
                var cognitoUser = await GetCognitoUser(id);
                var userId = cognitoUser.Attributes.ToArray()[0].Value;

                var followers = _context.UserFollowers
                                    .Where(u => u.UserId.Contains(userId))
                                    .Select(p => new { p.FollowerId });

                return Ok(new { followers = followers });
            }
            catch (UserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/following/id
        [HttpGet("following/{id}")]
        public async Task<IActionResult> GetFollowing(string id)
        {
            try
            {
                var cognitoUser = await GetCognitoUser(id);
                var userId = cognitoUser.Attributes.ToArray()[0].Value;

                var following = _context.UserFollowers
                                    .Where(u => u.FollowerId.Contains(userId))
                                    .Select(p => new { p.UserId });

                return Ok(new { following = following });
            }
            catch (UserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // Helper function to find a cognito user by id
        private async Task<UserType?> GetCognitoUser(string id)
        {
            ListUsersRequest listUsersRequest = new ListUsersRequest
            {
                UserPoolId = APP_CONFIG["UserPoolId"],
                Filter = "sub = \"" + id + "\""
            };

            var listUsersResponse = await cognitoIdClient.ListUsersAsync(listUsersRequest);

            if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                var users = listUsersResponse.Users;
                return users.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}

