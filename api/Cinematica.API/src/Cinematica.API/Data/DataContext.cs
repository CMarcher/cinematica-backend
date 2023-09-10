namespace Cinematica.API.Data;

using Cinematica.API.Models.Database;
using Microsoft.EntityFrameworkCore;

public partial class DataContext : DbContext
{
    protected readonly IConfiguration Configuration;

    public DataContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to postgres with connection string from app settings
        options.UseNpgsql(Configuration.GetConnectionString("cinematica-local"));
    }

    public virtual DbSet<CastMember> CastMembers { get; set; }
    public virtual DbSet<Like> Likes { get; set; }
    public virtual DbSet<MovieGenres> MovieGenres { get; set; }
    public virtual DbSet<MovieSelection> MovieSelections { get; set; }
    public virtual DbSet<MovieStudios> MovieStudios { get; set; }
    public virtual DbSet<DBMovie> Movies { get; set; }
    public virtual DbSet<Person> Persons { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<Studio> Studios { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserFollower> UserFollowers { get; set; }
    public virtual DbSet<UserMovie> UserMovies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CastMember>(entity =>
        {
            entity.ToTable("cast_members");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");

            entity.Property(e => e.PersonId).HasColumnName("person_id");

            entity.HasKey(e => new { e.MovieId, e.PersonId });

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("role");

            entity.HasOne(d => d.Person)
                .WithMany()
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cast_members_person_id_fkey");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("likes");

            entity.Property(e => e.PostId).HasColumnName("post_id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Post)
                .WithMany()
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("likes_post_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("likes_user_id_fkey");
        });

        modelBuilder.Entity<MovieGenres>(entity =>
        {
            entity.HasKey(g => new{g.MovieId, g.Genre});

            entity.ToTable("movie_genres");

            entity.Property(e => e.Genre)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("genre");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");

            entity.HasOne(d => d.Movie)
                .WithMany()
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movie_genres_movie_id_fkey");
        });

        modelBuilder.Entity<MovieSelection>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("movie_selections");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");

            entity.Property(e => e.PostId).HasColumnName("post_id");

            entity.HasOne(d => d.Post)
                .WithMany()
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movie_selections_post_id_fkey");
        });

        modelBuilder.Entity<MovieStudios>(entity =>
        {
            entity.HasKey(e => new { e.MovieId, e.StudioId });

            entity.ToTable("movie_studios");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");

            entity.Property(e => e.StudioId).HasColumnName("studio_id");

            entity.HasOne(d => d.Studio)
                .WithMany()
                .HasForeignKey(d => d.StudioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movie_studios_studio_id_fkey");
        });

        modelBuilder.Entity<DBMovie>(entity =>
        {
            entity.HasKey(e => e.MovieId)
                .HasName("movies_pkey");

            entity.ToTable("movies");

            entity.Property(e => e.MovieId)
                .ValueGeneratedNever()
                .HasColumnName("movie_id");

            entity.Property(e => e.Banner)
                .HasMaxLength(255)
                .HasColumnName("banner");

            entity.Property(e => e.Director)
                .HasMaxLength(255)
                .HasColumnName("director");

            entity.Property(e => e.Language)
                .HasMaxLength(255)
                .HasColumnName("language");

            entity.Property(e => e.Overview).HasColumnName("overview");

            entity.Property(e => e.Poster)
                .HasMaxLength(255)
                .HasColumnName("poster");

            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");

            entity.Property(e => e.RunningTime)
                .HasMaxLength(255)
                .HasColumnName("running_time");

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("person");

            entity.Property(e => e.PersonId)
                .ValueGeneratedNever()
                .HasColumnName("person_id");

            entity.Property(e => e.PersonName)
                .HasMaxLength(255)
                .HasColumnName("person_name");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");

            entity.Property(e => e.PostId)
                .ValueGeneratedNever()
                .HasColumnName("post_id");

            entity.Property(e => e.Body)
                .IsRequired()
                .HasColumnName("body");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.Property(e => e.ParentId).HasColumnName("parent_id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("posts_parent_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Posts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("posts_user_id_fkey");
        });

        modelBuilder.Entity<Studio>(entity =>
        {
            entity.ToTable("studios");

            entity.Property(e => e.StudioId)
                .ValueGeneratedNever()
                .HasColumnName("studio_id");

            entity.Property(e => e.StudioName)
                .HasMaxLength(255)
                .HasColumnName("studio_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.Property(e => e.CoverPicture)
                .HasMaxLength(255)
                .HasColumnName("cover_picture");

            entity.Property(e => e.ProfilePicture)
                .HasMaxLength(255)
                .HasColumnName("profile_picture");
        });

        modelBuilder.Entity<UserFollower>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("user_followers");

            entity.Property(e => e.FollowerId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("follower_id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Follower)
                .WithMany()
                .HasForeignKey(d => d.FollowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_followers_follower_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_followers_user_id_fkey");
        });

        modelBuilder.Entity<UserMovie>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("user_movies");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_movies_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

