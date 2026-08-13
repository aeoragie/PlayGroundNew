SET NOCOUNT ON;
-- 테스트 계정 10종 — 비밀번호 password123! (검증 계정 공통 해시 재사용). 로컬 전용, 운영 배포 금지.
DELETE FROM [dbo].[Users] WHERE [Email] IN ('team1@playground.com','player1@playground.com','player2@playground.com','team2@playground.com','player3@playground.com','player4@playground.com','team3@playground.com','player5@playground.com','team4@playground.com','player6@playground.com');
INSERT INTO [dbo].[Users] ([UserId],[Email],[EmailConfirmed],[PasswordHash],[AuthProvider],[DisplayName],[UserRole])
VALUES
('5FD683DC-09E7-C6EA-D514-1FBE70D99DBD','team1@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','전남순천중앙초 관리자','TeamAdmin'),
('3BEAC352-FB6C-D05F-3673-7245AE80C657','player1@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','정시우','Player'),
('7B839189-39CD-ECD4-3916-7694D3DCE328','player2@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','최원','Player'),
('78E67570-065B-F412-4B73-F1E6422F63B8','team2@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','경남FC스퀘어U12 관리자','TeamAdmin'),
('B487E506-DBF9-DE10-6448-AA2E77C0EC1B','player3@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','김홍석','Player'),
('0957543F-3257-AD8C-5458-05B51B102466','player4@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','변영준','Player'),
('A887EC09-49F2-F4D3-E0CA-3EBEBCE15ABE','team3@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','경남고성축구스포츠클럽U12 관리자','TeamAdmin'),
('11D0AAE6-4D54-B27D-56D4-3237742C424C','player5@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','허윤성','Player'),
('FCE52302-DFEF-855A-BCE5-F573FBB84135','team4@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','경남보물섬남해스포츠클럽U12 관리자','TeamAdmin'),
('E521FE52-A803-7FF8-EE6E-43C7205F7107','player6@playground.com',1,'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==','Local','김시영','Player');
GO
