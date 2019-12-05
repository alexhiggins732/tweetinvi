




CREATE View [dbo].[VwTopEng]

as

select top 100 
case when eng.UserId in 
(1339835893,-- hillary
910492003359760384, -- brenna
1179485989854744576, -- unkown? blocked?
1082334352711790593, --rep omar
783792992, --omar
30354991, --'harris'
25029495, --pulte
138203134, -- @AOC: Alexandria Ocasio-Cortez
26642006, -- @Alyssa_Milano: Alyssa Milano
15764644, -- @SpeakerPelosi: Nancy Pelosi
2334193741, --@Comey: James Comey
212973087, --@chunkymark
29501253 --shiff

) then eng.UserId else 
 act.TweetId
end
 as EngId, eng.* from VwEngagements eng
	left join VwMyActivityIds act on eng.TweetId= act.TweetId 
	--where act.TweetId is null
	order by epm desc
