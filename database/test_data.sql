INSERT INTO users (user_id, user_name) VALUES 
	-- adds jeffery
	('a33c0775-1406-4cc3-81ec-16151ecc4ade', 'jeffery'),
	-- adds david
	('93cfcbd6-54b6-4961-bec5-0cf6e0a81917', 'david'),
	-- adds test
	('e8f2aa40-b1a8-46f2-81c3-4e0dbc3e4f9d', 'test');

-- Run this block first and check the post_id to put into replies INSERT statement.
do $$
 declare 
  counter integer := 1;
 begin
  while counter <= 15 loop
    INSERT INTO posts (user_id, body) VALUES
	('a33c0775-1406-4cc3-81ec-16151ecc4ade', CONCAT('test', counter));
   counter := counter + 1;
  end loop;
 end$$;
 
select * from posts
-- Run this block to insert a reply and get reply_id to put into likes INSERT
do $$
 declare 
  counter integer := 1;
 begin
  while counter <= 15 loop
    INSERT INTO replies (user_id, post_id, body) VALUES
	('93cfcbd6-54b6-4961-bec5-0cf6e0a81917', 1, CONCAT('reply_test', counter));
   counter := counter + 1;
  end loop;
 end$$;

select * from replies;

-- Use reply_id and post_id found in previous statements
INSERT INTO likes (user_id, post_id, reply_id) VALUES
	('93cfcbd6-54b6-4961-bec5-0cf6e0a81917', 1, null);
	
INSERT INTO likes (user_id, post_id, reply_id) VALUES
	('93cfcbd6-54b6-4961-bec5-0cf6e0a81917', null, 1);
	
select * from likes;