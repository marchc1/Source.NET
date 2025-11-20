/*
Lua generated file.
Source:
	local AllLengths = {}

	local function GenInlineArray(i)
		print ("[InlineArray(" .. i .. ")] public struct InlineArray" .. i .. "<T> { public T item; }")  
		AllLengths[#AllLengths + 1] = i
	end

	print("using System.Runtime.CompilerServices;")
	print("")
	print("namespace Source;")

	for i = 1, 128 do GenInlineArray(i) end
	GenInlineArray(256)
	GenInlineArray(260)
	GenInlineArray(512)
	GenInlineArray(1024)
	GenInlineArray(2048)
	GenInlineArray(4096)
	GenInlineArray(8192)
*/

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Networking;

using System.Runtime.CompilerServices;

namespace Source;
[InlineArray(1)] public struct InlineArray1<T> { public T item; }
[InlineArray(2)] public struct InlineArray2<T> { public T item; }
[InlineArray(3)] public struct InlineArray3<T> { public T item; }
[InlineArray(4)] public struct InlineArray4<T> { public T item; }
[InlineArray(5)] public struct InlineArray5<T> { public T item; }
[InlineArray(6)] public struct InlineArray6<T> { public T item; }
[InlineArray(7)] public struct InlineArray7<T> { public T item; }
[InlineArray(8)] public struct InlineArray8<T> { public T item; }
[InlineArray(9)] public struct InlineArray9<T> { public T item; }
[InlineArray(10)] public struct InlineArray10<T> { public T item; }
[InlineArray(11)] public struct InlineArray11<T> { public T item; }
[InlineArray(12)] public struct InlineArray12<T> { public T item; }
[InlineArray(13)] public struct InlineArray13<T> { public T item; }
[InlineArray(14)] public struct InlineArray14<T> { public T item; }
[InlineArray(15)] public struct InlineArray15<T> { public T item; }
[InlineArray(16)] public struct InlineArray16<T> { public T item; }
[InlineArray(17)] public struct InlineArray17<T> { public T item; }
[InlineArray(18)] public struct InlineArray18<T> { public T item; }
[InlineArray(19)] public struct InlineArray19<T> { public T item; }
[InlineArray(20)] public struct InlineArray20<T> { public T item; }
[InlineArray(21)] public struct InlineArray21<T> { public T item; }
[InlineArray(22)] public struct InlineArray22<T> { public T item; }
[InlineArray(23)] public struct InlineArray23<T> { public T item; }
[InlineArray(24)] public struct InlineArray24<T> { public T item; }
[InlineArray(25)] public struct InlineArray25<T> { public T item; }
[InlineArray(26)] public struct InlineArray26<T> { public T item; }
[InlineArray(27)] public struct InlineArray27<T> { public T item; }
[InlineArray(28)] public struct InlineArray28<T> { public T item; }
[InlineArray(29)] public struct InlineArray29<T> { public T item; }
[InlineArray(30)] public struct InlineArray30<T> { public T item; }
[InlineArray(31)] public struct InlineArray31<T> { public T item; }
[InlineArray(32)] public struct InlineArray32<T> { public T item; }
[InlineArray(33)] public struct InlineArray33<T> { public T item; }
[InlineArray(34)] public struct InlineArray34<T> { public T item; }
[InlineArray(35)] public struct InlineArray35<T> { public T item; }
[InlineArray(36)] public struct InlineArray36<T> { public T item; }
[InlineArray(37)] public struct InlineArray37<T> { public T item; }
[InlineArray(38)] public struct InlineArray38<T> { public T item; }
[InlineArray(39)] public struct InlineArray39<T> { public T item; }
[InlineArray(40)] public struct InlineArray40<T> { public T item; }
[InlineArray(41)] public struct InlineArray41<T> { public T item; }
[InlineArray(42)] public struct InlineArray42<T> { public T item; }
[InlineArray(43)] public struct InlineArray43<T> { public T item; }
[InlineArray(44)] public struct InlineArray44<T> { public T item; }
[InlineArray(45)] public struct InlineArray45<T> { public T item; }
[InlineArray(46)] public struct InlineArray46<T> { public T item; }
[InlineArray(47)] public struct InlineArray47<T> { public T item; }
[InlineArray(48)] public struct InlineArray48<T> { public T item; }
[InlineArray(49)] public struct InlineArray49<T> { public T item; }
[InlineArray(50)] public struct InlineArray50<T> { public T item; }
[InlineArray(51)] public struct InlineArray51<T> { public T item; }
[InlineArray(52)] public struct InlineArray52<T> { public T item; }
[InlineArray(53)] public struct InlineArray53<T> { public T item; }
[InlineArray(54)] public struct InlineArray54<T> { public T item; }
[InlineArray(55)] public struct InlineArray55<T> { public T item; }
[InlineArray(56)] public struct InlineArray56<T> { public T item; }
[InlineArray(57)] public struct InlineArray57<T> { public T item; }
[InlineArray(58)] public struct InlineArray58<T> { public T item; }
[InlineArray(59)] public struct InlineArray59<T> { public T item; }
[InlineArray(60)] public struct InlineArray60<T> { public T item; }
[InlineArray(61)] public struct InlineArray61<T> { public T item; }
[InlineArray(62)] public struct InlineArray62<T> { public T item; }
[InlineArray(63)] public struct InlineArray63<T> { public T item; }
[InlineArray(64)] public struct InlineArray64<T> { public T item; }
[InlineArray(65)] public struct InlineArray65<T> { public T item; }
[InlineArray(66)] public struct InlineArray66<T> { public T item; }
[InlineArray(67)] public struct InlineArray67<T> { public T item; }
[InlineArray(68)] public struct InlineArray68<T> { public T item; }
[InlineArray(69)] public struct InlineArray69<T> { public T item; }
[InlineArray(70)] public struct InlineArray70<T> { public T item; }
[InlineArray(71)] public struct InlineArray71<T> { public T item; }
[InlineArray(72)] public struct InlineArray72<T> { public T item; }
[InlineArray(73)] public struct InlineArray73<T> { public T item; }
[InlineArray(74)] public struct InlineArray74<T> { public T item; }
[InlineArray(75)] public struct InlineArray75<T> { public T item; }
[InlineArray(76)] public struct InlineArray76<T> { public T item; }
[InlineArray(77)] public struct InlineArray77<T> { public T item; }
[InlineArray(78)] public struct InlineArray78<T> { public T item; }
[InlineArray(79)] public struct InlineArray79<T> { public T item; }
[InlineArray(80)] public struct InlineArray80<T> { public T item; }
[InlineArray(81)] public struct InlineArray81<T> { public T item; }
[InlineArray(82)] public struct InlineArray82<T> { public T item; }
[InlineArray(83)] public struct InlineArray83<T> { public T item; }
[InlineArray(84)] public struct InlineArray84<T> { public T item; }
[InlineArray(85)] public struct InlineArray85<T> { public T item; }
[InlineArray(86)] public struct InlineArray86<T> { public T item; }
[InlineArray(87)] public struct InlineArray87<T> { public T item; }
[InlineArray(88)] public struct InlineArray88<T> { public T item; }
[InlineArray(89)] public struct InlineArray89<T> { public T item; }
[InlineArray(90)] public struct InlineArray90<T> { public T item; }
[InlineArray(91)] public struct InlineArray91<T> { public T item; }
[InlineArray(92)] public struct InlineArray92<T> { public T item; }
[InlineArray(93)] public struct InlineArray93<T> { public T item; }
[InlineArray(94)] public struct InlineArray94<T> { public T item; }
[InlineArray(95)] public struct InlineArray95<T> { public T item; }
[InlineArray(96)] public struct InlineArray96<T> { public T item; }
[InlineArray(97)] public struct InlineArray97<T> { public T item; }
[InlineArray(98)] public struct InlineArray98<T> { public T item; }
[InlineArray(99)] public struct InlineArray99<T> { public T item; }
[InlineArray(100)] public struct InlineArray100<T> { public T item; }
[InlineArray(101)] public struct InlineArray101<T> { public T item; }
[InlineArray(102)] public struct InlineArray102<T> { public T item; }
[InlineArray(103)] public struct InlineArray103<T> { public T item; }
[InlineArray(104)] public struct InlineArray104<T> { public T item; }
[InlineArray(105)] public struct InlineArray105<T> { public T item; }
[InlineArray(106)] public struct InlineArray106<T> { public T item; }
[InlineArray(107)] public struct InlineArray107<T> { public T item; }
[InlineArray(108)] public struct InlineArray108<T> { public T item; }
[InlineArray(109)] public struct InlineArray109<T> { public T item; }
[InlineArray(110)] public struct InlineArray110<T> { public T item; }
[InlineArray(111)] public struct InlineArray111<T> { public T item; }
[InlineArray(112)] public struct InlineArray112<T> { public T item; }
[InlineArray(113)] public struct InlineArray113<T> { public T item; }
[InlineArray(114)] public struct InlineArray114<T> { public T item; }
[InlineArray(115)] public struct InlineArray115<T> { public T item; }
[InlineArray(116)] public struct InlineArray116<T> { public T item; }
[InlineArray(117)] public struct InlineArray117<T> { public T item; }
[InlineArray(118)] public struct InlineArray118<T> { public T item; }
[InlineArray(119)] public struct InlineArray119<T> { public T item; }
[InlineArray(120)] public struct InlineArray120<T> { public T item; }
[InlineArray(121)] public struct InlineArray121<T> { public T item; }
[InlineArray(122)] public struct InlineArray122<T> { public T item; }
[InlineArray(123)] public struct InlineArray123<T> { public T item; }
[InlineArray(124)] public struct InlineArray124<T> { public T item; }
[InlineArray(125)] public struct InlineArray125<T> { public T item; }
[InlineArray(126)] public struct InlineArray126<T> { public T item; }
[InlineArray(127)] public struct InlineArray127<T> { public T item; }
[InlineArray(128)] public struct InlineArray128<T> { public T item; }
[InlineArray(255)] public struct InlineArray255<T> { public T item; }
[InlineArray(256)] public struct InlineArray256<T> { public T item; }
[InlineArray(260)] public struct InlineArray260<T> { public T item; }
[InlineArray(512)] public struct InlineArray512<T> { public T item; }
[InlineArray(1024)] public struct InlineArray1024<T> { public T item; }
[InlineArray(2048)] public struct InlineArray2048<T> { public T item; }
[InlineArray(4096)] public struct InlineArray4096<T> { public T item; }
[InlineArray(8192)] public struct InlineArray8192<T> { public T item; }


// Very specialized uses
[InlineArray(Constants.MAX_EDICTS)] public struct InlineArrayMaxEdicts<T> { public T item; }
[InlineArray(MAX_PATH)] public struct InlineArrayMaxPath<T> { public T item; }
[InlineArray(Constants.MAX_PACKEDENTITY_DATA)] public struct InlineArrayMaxPackedEntityData<T> { public T item; }
[InlineArray(Constants.NUM_ENT_ENTRIES)] public struct InlineArrayNumEntEntries<T> { public T item; }
[InlineArray(Protocol.STEAM_KEYSIZE)] public struct InlineArraySteamKeysize<T> { public T item; }
[InlineArray(Constants.DT_MAX_STRING_BUFFERSIZE)] public struct InlineArrayDtMaxStringBufferSize<T> { public T item; }
[InlineArray(Constants.MAX_TOTAL_SENDTABLE_PROPS)] public struct InlineArrayMaxTotalSendTableProps<T> { public T item; }
[InlineArray(Studio.MAXSTUDIOPOSEPARAM)] public struct InlineArrayMaxStudioPoseParam<T> { public T item; public int Length => Studio.MAXSTUDIOPOSEPARAM; }
[InlineArray(Studio.MAXSTUDIOBONECTRLS)] public struct InlineArrayMaxStudioBoneCtrls<T> { public T item; }
[InlineArray(Constants.MAX_AREA_STATE_BYTES)] public struct InlineArrayMaxAreaStateBytes<T> { public T item; }
[InlineArray(Constants.MAX_AREA_PORTAL_STATE_BYTES)] public struct InlineArrayMaxAreaPortalStateBytes<T> { public T item; }
[InlineArray(BSPFileCommon.MAXLIGHTMAPS)] public struct InlineArrayMaxLightmaps<T> { public T item; }

// Super specific type needed. Create these as necessary. These new up their values on construction
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
[InlineArray(32)]
public struct InlineArrayNew32<T> where T : new()
{
	public T item;
	public InlineArrayNew32() { for (int i = 0; i < 32; i++) this[i] = new(); }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
