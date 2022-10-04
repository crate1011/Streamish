using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Streamish.Models;
using Streamish.Utils;

namespace Streamish.Repositories
{

	public class UserProfileRepository : BaseRepository, IUserProfileRepository
	{
		public UserProfileRepository(IConfiguration configuration) : base(configuration) { }

		public List<UserProfile> GetAll()
		{
			using (var conn = Connection)
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = @"
					SELECT Id, Name, Email, ImageUrl, DateCreated
                        
					FROM UserProfile
					ORDER BY DateCreated";

					using (SqlDataReader reader = cmd.ExecuteReader())
					{

						var videos = new List<UserProfile>();
						while (reader.Read())
						{
							videos.Add(new UserProfile()
							{
								Id = DbUtils.GetInt(reader, "Id"),
								Name = DbUtils.GetString(reader, "Name"),
								Email = DbUtils.GetString(reader, "Email"),
								DateCreated = DbUtils.GetDateTime(reader, "DateCreated"),
								ImageUrl = DbUtils.GetString(reader, "ImageUrl"),
							});
						}

						return videos;
					}
				}
			}
		}

		public void Add(UserProfile userProfile)
		{
			using (var conn = Connection)
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = @"
                        INSERT INTO UserProfile (Name, Email, DateCreated, ImageUrl)
                        OUTPUT INSERTED.ID
                        VALUES (@Name, @Email, @DateCreated, @ImageUrl)";

					DbUtils.AddParameter(cmd, "@Name", userProfile.Name);
					DbUtils.AddParameter(cmd, "@Email", userProfile.Email);
					DbUtils.AddParameter(cmd, "@DateCreated", userProfile.DateCreated);
					DbUtils.AddParameter(cmd, "@ImageUrl", userProfile.ImageUrl);

					userProfile.Id = (int)cmd.ExecuteScalar();
				}
			}
		}

		public void Update(UserProfile userProfile)
		{
			using (var conn = Connection)
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = @"
                        UPDATE UserProfile
                           SET Name = @Name,
                               Email = @Email,
                               DateCreated = @DateCreated,
                               ImageUrl = @ImageUrl,
                         WHERE Id = @Id";

					DbUtils.AddParameter(cmd, "@Name", userProfile.Name);
					DbUtils.AddParameter(cmd, "@Email", userProfile.Email);
					DbUtils.AddParameter(cmd, "@DateCreated", userProfile.DateCreated);
					DbUtils.AddParameter(cmd, "@ImageUrl", userProfile.ImageUrl);
					DbUtils.AddParameter(cmd, "@Id", userProfile.Id);

					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var conn = Connection)
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "DELETE FROM UserProfile WHERE Id = @Id";
					DbUtils.AddParameter(cmd, "@id", id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public UserProfile GetUserByIdWithVideos(int id)
		{
			using (var conn = Connection)
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = @"
                         SELECT v.Id AS VideoId, v.Title, v.Description, v.Url, v.DateCreated AS VideoDateCreated, 
						 up.Id AS UserProfileId, up.Name AS UserName, up.Email AS UserEmail, 
						 up.ImageUrl UserProfileImageUrl, up.DateCreated AS UserProfileDateCreated,
						 c.Id AS CommentId, c.Message, c.UserProfileId AS CommentUserProfileId, c.VideoId
						 FROM Video v
						 LEFT JOIN UserProfile up ON v.UserProfileId = up.Id
						 LEFT JOIN Comment c ON c.VideoId = v.Id
						 WHERE up.Id = @id";

					DbUtils.AddParameter(cmd, "@Id", id);

					var reader = cmd.ExecuteReader();

					

						UserProfile userProfile = null;
					while (reader.Read())
					{


						if (userProfile == null)
						{
							userProfile = new UserProfile()
							{
								Id = DbUtils.GetInt(reader, "UserProfileId"),
								Name = DbUtils.GetString(reader, "UserName"),
								Email = DbUtils.GetString(reader, "UserEmail"),
								DateCreated = DbUtils.GetDateTime(reader, "UserProfileDateCreated"),
								ImageUrl = DbUtils.GetString(reader, "UserProfileImageUrl"),
								AuthoredVideos = new List<Video>()
							};
						}

						if (DbUtils.IsNotDbNull(reader, "UserProfileId"))
						{
							var videoId = DbUtils.GetInt(reader, "VideoId");
							var video = userProfile.AuthoredVideos.Find(p => p.Id == videoId);

							if (video == null)
							{
								video = new Video()
								{
									Id = DbUtils.GetInt(reader, "VideoId"),
									Title = DbUtils.GetString(reader, "Title"),
									Description = DbUtils.GetString(reader, "Description"),
									Url = DbUtils.GetString(reader, "Url"),
									DateCreated = DbUtils.GetDateTime(reader, "VideoDateCreated"),
									UserProfileId = DbUtils.GetInt(reader, "UserProfileId"),
									Comments = new List<Comment>()
								};
								userProfile.AuthoredVideos.Add(video);
							}

							if (DbUtils.IsNotDbNull(reader, "CommentId"))
							{
								video.Comments.Add(new Comment()
								{
									Id = DbUtils.GetInt(reader, "CommentId"),
									Message = DbUtils.GetString(reader, "Message"),
									VideoId = DbUtils.GetInt(reader, "VideoId"),
									UserProfileId = DbUtils.GetInt(reader, "CommentUserProfileId")
								});
							};
						}
					};
					reader.Close();
					return userProfile;
				}
			}
		}

	}
}