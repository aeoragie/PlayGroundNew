-- 한글 이름 → URL 슬러그용 로마자(ASCII) 변환. 국립국어원 로마자 표기(RR)를 음절 단위로 적용.
--   · 성씨 관용표(김→kim 등)는 쓰지 않는다 — 단일 이름/성+이름 구분이 불가능해 첫 음절을 성씨로
--     오판하는 모호성이 생긴다. 순수 RR이 결정적이고(강민→gangmin), 슬러그 용도엔 충분하다.
--   · 한글 음절은 초/중/종성으로 분해해 RR 매핑, ASCII 영숫자는 소문자로 통과, 그 외(공백·기호)는 제거.
--   · 반환은 base 슬러그(중복 접미사 -2 없음). 빈 문자열이면 호출측이 'player'/'team'으로 대체한다.
--   · UTF-8 콜레이션 전제 — SUBSTRING/UNICODE가 바이트가 아니라 코드포인트(문자) 단위로 동작한다.
CREATE FUNCTION [dbo].[UfnRomanizeKoreanSlug] (@text VARCHAR(300))
RETURNS VARCHAR(150)
AS
BEGIN
    DECLARE @out  VARCHAR(300) = '';
    DECLARE @i    INT = 1;
    DECLARE @len  INT = LEN(ISNULL(@text, ''));
    DECLARE @code INT;
    DECLARE @s    INT;
    DECLARE @cho  INT;
    DECLARE @jung INT;
    DECLARE @jong INT;

    WHILE @i <= @len
    BEGIN
        SET @code = UNICODE(SUBSTRING(@text, @i, 1));

        IF @code BETWEEN 44032 AND 55203   -- 한글 음절 블록 (U+AC00 ~ U+D7A3)
        BEGIN
            SET @s    = @code - 44032;
            SET @cho  = @s / 588;
            SET @jung = (@s % 588) / 28;
            SET @jong = @s % 28;

            SET @out = @out
                + CASE @cho
                    WHEN 0 THEN 'g'  WHEN 1 THEN 'kk' WHEN 2 THEN 'n'  WHEN 3 THEN 'd'  WHEN 4 THEN 'tt'
                    WHEN 5 THEN 'r'  WHEN 6 THEN 'm'  WHEN 7 THEN 'b'  WHEN 8 THEN 'pp' WHEN 9 THEN 's'
                    WHEN 10 THEN 'ss' WHEN 11 THEN ''  WHEN 12 THEN 'j' WHEN 13 THEN 'jj' WHEN 14 THEN 'ch'
                    WHEN 15 THEN 'k' WHEN 16 THEN 't' WHEN 17 THEN 'p' WHEN 18 THEN 'h' ELSE '' END
                + CASE @jung
                    WHEN 0 THEN 'a'  WHEN 1 THEN 'ae' WHEN 2 THEN 'ya' WHEN 3 THEN 'yae' WHEN 4 THEN 'eo'
                    WHEN 5 THEN 'e'  WHEN 6 THEN 'yeo' WHEN 7 THEN 'ye' WHEN 8 THEN 'o'  WHEN 9 THEN 'wa'
                    WHEN 10 THEN 'wae' WHEN 11 THEN 'oe' WHEN 12 THEN 'yo' WHEN 13 THEN 'u' WHEN 14 THEN 'wo'
                    WHEN 15 THEN 'we' WHEN 16 THEN 'wi' WHEN 17 THEN 'yu' WHEN 18 THEN 'eu' WHEN 19 THEN 'ui'
                    WHEN 20 THEN 'i' ELSE '' END
                + CASE @jong
                    WHEN 0 THEN ''  WHEN 1 THEN 'k' WHEN 2 THEN 'k' WHEN 3 THEN 'k' WHEN 4 THEN 'n'
                    WHEN 5 THEN 'n' WHEN 6 THEN 'n' WHEN 7 THEN 't' WHEN 8 THEN 'l' WHEN 9 THEN 'k'
                    WHEN 10 THEN 'm' WHEN 11 THEN 'l' WHEN 12 THEN 'l' WHEN 13 THEN 'l' WHEN 14 THEN 'p'
                    WHEN 15 THEN 'l' WHEN 16 THEN 'm' WHEN 17 THEN 'p' WHEN 18 THEN 'p' WHEN 19 THEN 't'
                    WHEN 20 THEN 't' WHEN 21 THEN 'ng' WHEN 22 THEN 't' WHEN 23 THEN 't' WHEN 24 THEN 'k'
                    WHEN 25 THEN 't' WHEN 26 THEN 'p' WHEN 27 THEN 't' ELSE '' END;
        END
        ELSE IF (@code BETWEEN 48 AND 57)   -- 0-9
             OR (@code BETWEEN 65 AND 90)   -- A-Z
             OR (@code BETWEEN 97 AND 122)  -- a-z
        BEGIN
            SET @out = @out + LOWER(SUBSTRING(@text, @i, 1));
        END
        -- 그 외(공백·기호·기타 유니코드)는 버린다.

        SET @i = @i + 1;
    END

    RETURN LEFT(@out, 140);
END
