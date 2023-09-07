/*
DROP TABLE likes
DROP TABLE movie_selections
DROP TABLE user_movies
DROP TABLE user_followers
DROP TABLE posts
DROP TABLE users
DROP TABLE shows
*/


CREATE TABLE users
(
    user_id varchar(255),
    profile_picture varchar(255),
    cover_photo varchar(255),
    PRIMARY KEY (user_id)
);

CREATE TABLE movies
(
    movie_id integer,
    title varchar(255) not null,
    img varchar(255),
	summary text,
    PRIMARY KEY (movie_id)
);

CREATE TABLE posts
(
    post_id bigint,
	parent_id bigint,
    user_id varchar(255) not null,
    created_at timestamptz not null,
	body text not null,
    PRIMARY KEY (post_id),
	FOREIGN KEY (parent_id) references posts (post_id),
	FOREIGN KEY (user_id) references users (user_id)
);

CREATE TABLE user_followers (
	user_id varchar(255) not null,
	follower_id varchar(255) not null,
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (follower_id) references users (user_id)
);

CREATE TABLE user_movies (
	user_id varchar(255) not null,
	movie_id int not null,
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (movie_id) references movies (movie_id)
);

CREATE TABLE likes (
	user_id varchar(255) not null,
	post_id bigint not null,
	FOREIGN KEY (user_id) references users (user_id),
	FOREIGN KEY (post_id) references posts (post_id)
);

CREATE TABLE movie_selections (
	post_id bigint not null,
	movie_id int not null,
	FOREIGN KEY (post_id) references posts (post_id),
	FOREIGN KEY (movie_id) references movies (movie_id)
)