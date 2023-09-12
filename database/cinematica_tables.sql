/*
DROP TABLE likes;
DROP TABLE movie_selections;
DROP TABLE user_movies;
DROP TABLE user_followers;
DROP TABLE movie_genres;
DROP TABLE movie_studios;
DROP TABLE cast_members;
DROP TABLE replies;
DROP TABLE posts;
DROP TABLE person;
DROP TABLE studios;
DROP TABLE movies;
DROP TABLE users;
*/


CREATE TABLE users
(
	user_id varchar(255),
	profile_picture varchar(255),
	cover_picture varchar(255),
	PRIMARY KEY (user_id)
);

CREATE TABLE movies
(
   	movie_id integer,
	title varchar(255) not null,
	release_date date,
	director varchar(255),
	poster varchar(255),
	banner varchar(255),
	language varchar(255),
	running_time varchar(255),
	overview text,
	PRIMARY KEY (movie_id)
);

CREATE TABLE person
(
	person_id int,
	person_name varchar(255) not null,
	PRIMARY KEY (person_id)
);

CREATE TABLE studios
(
	studio_id int,
	studio_name varchar(255) not null,
	PRIMARY KEY (studio_id)
);

CREATE TABLE posts
(
	post_id bigserial NOT null,
	user_id varchar(255) not null,
	created_at timestamptz DEFAULT NOW() not null,
	body text not null,
	image varchar(255),
	is_spoiler bool DEFAULT false not null,
	PRIMARY KEY (post_id),
	FOREIGN KEY (user_id) references users (user_id)
);

CREATE TABLE replies
(
	reply_id bigserial NOT null,
	post_id bigint Not null,
	user_id varchar(255) not null,
	created_at timestamptz DEFAULT NOW() not null ,
	body text not null,
	PRIMARY KEY (reply_id),
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (post_id) references posts(post_id)
);

CREATE TABLE user_followers (
	user_id varchar(255) not null,
	follower_id varchar(255) not null,
	PRIMARY KEY(user_id, follower_id),
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (follower_id) references users (user_id)
);

CREATE TABLE user_movies (
	user_id varchar(255) not null,
	movie_id int not null,
	PRIMARY KEY(user_id, movie_id),
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (movie_id) references movies (movie_id)
);

CREATE TABLE likes (
	like_id bigserial NOT null,
	user_id varchar(255) not null,
	post_id bigint,
	reply_id bigint,
	CHECK ((post_id IS null) != (reply_id is null)),
	
	PRIMARY KEY (like_id),
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (post_id) references posts (post_id),
	FOREIGN KEY (reply_id) references replies (reply_id)
);

CREATE UNIQUE INDEX on likes(user_id,post_id);
CREATE UNIQUE INDEX on likes(user_id,reply_id);

CREATE TABLE movie_selections (
	post_id bigint not null,
	movie_id int not null,
	PRIMARY KEY(post_id, movie_id),
	FOREIGN KEY (post_id) references posts (post_id),
	FOREIGN KEY (movie_id) references movies (movie_id)
);

CREATE TABLE movie_genres (
	movie_id int not null,
	genre varchar(255) not null,
	PRIMARY KEY(movie_id, genre),
	FOREIGN KEY (movie_id) references movies (movie_id)
);

CREATE TABLE movie_studios (
	movie_id int not null,
	studio_id int not null,
	PRIMARY KEY(movie_id, studio_id),
	FOREIGN KEY (movie_id) references movies (movie_id),
	FOREIGN KEY (studio_id) references studios (studio_id)
);

CREATE TABLE cast_members (
	movie_id int not null,
	person_id int not null,
	role varchar(255) not null,
	PRIMARY KEY(movie_id, person_id),
	FOREIGN KEY (movie_id) references movies (movie_id),
	FOREIGN KEY (person_id) references person (person_id)
);