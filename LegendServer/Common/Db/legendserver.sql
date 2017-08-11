/*
Navicat MySQL Data Transfer

Source Server         : legendserver
Source Server Version : 50624
Source Host           : localhost:3306
Source Database       : legendserver

Target Server Type    : MYSQL
Target Server Version : 50624
File Encoding         : 65001

Date: 2017-08-04 16:28:37
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for `activitiessystemconfig`
-- ----------------------------
DROP TABLE IF EXISTS `activitiessystemconfig`;
CREATE TABLE `activitiessystemconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(1024) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of activitiessystemconfig
-- ----------------------------
INSERT INTO `activitiessystemconfig` VALUES ('CompetitionApplyTime', '3', '比赛场报名存在时间(小时)');
INSERT INTO `activitiessystemconfig` VALUES ('CompetitionCreateLimitNum', '5', '商家创建比赛场限制');
INSERT INTO `activitiessystemconfig` VALUES ('CompetitionEndTime', '3', '比赛场结束存在时间(天)');
INSERT INTO `activitiessystemconfig` VALUES ('CompetitionPlayerPageNum', '20', '活动比赛场请求玩家每页数量');
INSERT INTO `activitiessystemconfig` VALUES ('KeyUsedLimit', '1', '玩家活动专场口令使用次数限制');
INSERT INTO `activitiessystemconfig` VALUES ('Version', '1', '总版本号');

-- ----------------------------
-- Table structure for `agentharvestconfig`
-- ----------------------------
DROP TABLE IF EXISTS `agentharvestconfig`;
CREATE TABLE `agentharvestconfig` (
  `agent_type` varchar(1) NOT NULL,
  `agent_percent` varchar(3) NOT NULL,
  `mdy_people` varchar(32) DEFAULT NULL,
  `mdy_date` varchar(32) DEFAULT NULL,
  `total_recharge_agent` int(32) DEFAULT '0',
  `increase_percent` varchar(32) DEFAULT '0',
  `recharge_people` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`agent_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of agentharvestconfig
-- ----------------------------
INSERT INTO `agentharvestconfig` VALUES ('1', '50', 'root', '2017/6/12 15:16:47', '0', '5', null);
INSERT INTO `agentharvestconfig` VALUES ('2', '40', 'root', '2017/6/12 15:16:52', '0', '5', null);

-- ----------------------------
-- Table structure for `bindwx`
-- ----------------------------
DROP TABLE IF EXISTS `bindwx`;
CREATE TABLE `bindwx` (
  `summonerId` bigint(64) unsigned NOT NULL,
  `account` varchar(32) NOT NULL,
  `wx_uid` varchar(32) NOT NULL,
  PRIMARY KEY (`summonerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of bindwx
-- ----------------------------

-- ----------------------------
-- Table structure for `companyinfo`
-- ----------------------------
DROP TABLE IF EXISTS `companyinfo`;
CREATE TABLE `companyinfo` (
  `name` varchar(32) NOT NULL,
  `boss` varchar(32) NOT NULL,
  `corporation` varchar(32) NOT NULL DEFAULT '',
  `tel` varchar(32) NOT NULL DEFAULT '',
  `address` varchar(128) NOT NULL,
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of companyinfo
-- ----------------------------

-- ----------------------------
-- Table structure for `dbcachesynctest`
-- ----------------------------
DROP TABLE IF EXISTS `dbcachesynctest`;
CREATE TABLE `dbcachesynctest` (
  `guid` bigint(64) unsigned NOT NULL,
  `data` blob NOT NULL,
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of dbcachesynctest
-- ----------------------------

-- ----------------------------
-- Table structure for `employeeinfo`
-- ----------------------------
DROP TABLE IF EXISTS `employeeinfo`;
CREATE TABLE `employeeinfo` (
  `account` varchar(32) NOT NULL,
  `password` varchar(32) NOT NULL DEFAULT '1234',
  `summonerId` bigint(64) NOT NULL DEFAULT '0',
  `area` varchar(32) NOT NULL DEFAULT '1',
  `jobTitle` int(32) NOT NULL DEFAULT '0',
  `tel` varchar(32) NOT NULL,
  `company` varchar(32) NOT NULL,
  `leader` varchar(32) NOT NULL,
  `myJunniors` blob NOT NULL,
  `sex` varchar(1) DEFAULT NULL,
  `card_no` varchar(18) DEFAULT NULL,
  `bank_no` varchar(19) DEFAULT NULL,
  `bank_user_name` varchar(32) DEFAULT NULL,
  `em_status` varchar(1) DEFAULT NULL,
  `bank_name` varchar(32) DEFAULT NULL,
  `referee_id` bigint(64) DEFAULT NULL,
  `crete_date` varchar(20) DEFAULT NULL,
  `em_email` varchar(32) DEFAULT NULL,
  `shopkeeper_type` char(1) DEFAULT '2',
  PRIMARY KEY (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of employeeinfo
-- ----------------------------

-- ----------------------------
-- Table structure for `employeelog`
-- ----------------------------
DROP TABLE IF EXISTS `employeelog`;
CREATE TABLE `employeelog` (
  `logId` bigint(64) unsigned NOT NULL DEFAULT '0',
  `recorder` varchar(32) NOT NULL,
  `recorderJobTitle` varchar(32) NOT NULL,
  `time` datetime NOT NULL,
  `reason` varchar(64) NOT NULL,
  PRIMARY KEY (`logId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of employeelog
-- ----------------------------

-- ----------------------------
-- Table structure for `employee_auditing`
-- ----------------------------
DROP TABLE IF EXISTS `employee_auditing`;
CREATE TABLE `employee_auditing` (
  `account` varchar(32) NOT NULL DEFAULT '',
  `summonerId` bigint(64) unsigned NOT NULL,
  `status` varchar(1) NOT NULL DEFAULT '0',
  `refusal_reason` varchar(200) DEFAULT NULL,
  `create_time` varchar(20) DEFAULT NULL,
  `modify_time` varchar(20) DEFAULT NULL,
  `create_people` varchar(32) DEFAULT NULL,
  `modify_people` varchar(32) DEFAULT NULL,
  PRIMARY KEY (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of employee_auditing
-- ----------------------------

-- ----------------------------
-- Table structure for `feedback`
-- ----------------------------
DROP TABLE IF EXISTS `feedback`;
CREATE TABLE `feedback` (
  `id` bigint(64) unsigned NOT NULL,
  `context` varchar(255) NOT NULL,
  `phoneNumber` varchar(32) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`id`,`context`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of feedback
-- ----------------------------

-- ----------------------------
-- Table structure for `gm`
-- ----------------------------
DROP TABLE IF EXISTS `gm`;
CREATE TABLE `gm` (
  `account` varchar(32) NOT NULL,
  `password` varchar(32) NOT NULL DEFAULT '1234',
  `auth` int(32) NOT NULL DEFAULT '0',
  `roomCard` int(32) NOT NULL DEFAULT '1',
  PRIMARY KEY (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of gm
-- ----------------------------

-- ----------------------------
-- Table structure for `gmlog`
-- ----------------------------
DROP TABLE IF EXISTS `gmlog`;
CREATE TABLE `gmlog` (
  `logId` bigint(64) unsigned NOT NULL DEFAULT '0',
  `recorder` varchar(32) NOT NULL,
  `recorderAuth` varchar(32) NOT NULL DEFAULT '',
  `time` datetime NOT NULL,
  `reason` varchar(64) NOT NULL,
  PRIMARY KEY (`logId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of gmlog
-- ----------------------------

-- ----------------------------
-- Table structure for `logdebug`
-- ----------------------------
DROP TABLE IF EXISTS `logdebug`;
CREATE TABLE `logdebug` (
  `logID` bigint(64) unsigned NOT NULL,
  `serverName` varchar(32) NOT NULL,
  `serverID` int(32) NOT NULL,
  `context` varchar(512) NOT NULL,
  PRIMARY KEY (`logID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of logdebug
-- ----------------------------

-- ----------------------------
-- Table structure for `logerror`
-- ----------------------------
DROP TABLE IF EXISTS `logerror`;
CREATE TABLE `logerror` (
  `logID` bigint(64) unsigned NOT NULL,
  `serverName` varchar(32) NOT NULL,
  `serverID` int(32) NOT NULL,
  `context` varchar(512) NOT NULL,
  PRIMARY KEY (`logID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of logerror
-- ----------------------------

-- ----------------------------
-- Table structure for `logfatal`
-- ----------------------------
DROP TABLE IF EXISTS `logfatal`;
CREATE TABLE `logfatal` (
  `logID` bigint(64) unsigned NOT NULL,
  `serverName` varchar(32) NOT NULL,
  `serverID` int(32) NOT NULL,
  `context` varchar(512) NOT NULL,
  PRIMARY KEY (`logID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of logfatal
-- ----------------------------

-- ----------------------------
-- Table structure for `loginfo`
-- ----------------------------
DROP TABLE IF EXISTS `loginfo`;
CREATE TABLE `loginfo` (
  `logID` bigint(64) unsigned NOT NULL,
  `serverName` varchar(32) NOT NULL,
  `serverID` int(32) NOT NULL,
  `context` varchar(512) NOT NULL,
  PRIMARY KEY (`logID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of loginfo
-- ----------------------------

-- ----------------------------
-- Table structure for `mahjongbureau`
-- ----------------------------
DROP TABLE IF EXISTS `mahjongbureau`;
CREATE TABLE `mahjongbureau` (
  `houseId` bigint(64) unsigned NOT NULL,
  `bureau` bigint(64) unsigned NOT NULL,
  `playerinfo` blob NOT NULL,
  `playermahjong` blob NOT NULL,
  `showmahjong` blob NOT NULL,
  `bureauTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`,`bureau`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of mahjongbureau
-- ----------------------------

-- ----------------------------
-- Table structure for `mahjongconfig`
-- ----------------------------
DROP TABLE IF EXISTS `mahjongconfig`;
CREATE TABLE `mahjongconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(128) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of mahjongconfig
-- ----------------------------
INSERT INTO `mahjongconfig` VALUES ('CSMahjongPingWinIntegral', '1', '长沙麻将小胡基础积分');
INSERT INTO `mahjongconfig` VALUES ('CSMahjongSpecialWinIntegral', '6', '长沙麻将大胡基础积分');
INSERT INTO `mahjongconfig` VALUES ('CSMahjongStartDisplayIntegral', '1', '长沙麻将摆牌基础积分');
INSERT INTO `mahjongconfig` VALUES ('MahjongBureauByHouseCard', '8,1|16,2', '麻将开局数对应的房卡消耗');
INSERT INTO `mahjongconfig` VALUES ('MahjongCatchBirdCount', '6', '麻将商家模式抓鸟个数');
INSERT INTO `mahjongconfig` VALUES ('MahjongDoubleKong', 'true', '麻将商家模式是否双倍开杠');
INSERT INTO `mahjongconfig` VALUES ('MahjongDoubleSeabed', 'true', '麻将商家模式是否双倍海底');
INSERT INTO `mahjongconfig` VALUES ('MahjongEatHu', 'true', '麻将商家模式是否吃胡');
INSERT INTO `mahjongconfig` VALUES ('MahjongFakeHu', 'true', '麻将商家模式是否假将胡');
INSERT INTO `mahjongconfig` VALUES ('MahjongFlutterType', '2', '麻将商家模式飘(0-不飘,1-飘1分,2-飘2分)');
INSERT INTO `mahjongconfig` VALUES ('MahjongGrabKongHu', 'true', '麻将商家模式是否抢杠胡');
INSERT INTO `mahjongconfig` VALUES ('MahjongHu7Pairs', 'true', '麻将商家模式是否胡7对');
INSERT INTO `mahjongconfig` VALUES ('MahjongHuZhuang', 'true', '麻将商家模式是否胡牌为庄');
INSERT INTO `mahjongconfig` VALUES ('MahjongIntegralCapped', '42', '麻将积分封顶数');
INSERT INTO `mahjongconfig` VALUES ('MahjongJin', 'false', '麻将商家模式是否算筋');
INSERT INTO `mahjongconfig` VALUES ('MahjongMaxBureau', '4', '麻将商家模式最大对局数');
INSERT INTO `mahjongconfig` VALUES ('MahjongMaxPlayerNum', '4', '麻将商家模式最大人数');
INSERT INTO `mahjongconfig` VALUES ('MahjongOpenIntegralCapped', 'true', '麻将商家模式是否积分上限');
INSERT INTO `mahjongconfig` VALUES ('MahjongOpenPersonalisePendulum', 'true', '麻将商家模式是否开启个性摆牌');
INSERT INTO `mahjongconfig` VALUES ('MahjongType', '1', '麻将商家模式麻将类型(1-长沙,2-转转,3-红中)');
INSERT INTO `mahjongconfig` VALUES ('MahjongWinBirdType', '1', '麻将商家模式中鸟类型(1-鸟翻倍,2-鸟加分)');
INSERT INTO `mahjongconfig` VALUES ('MahjongZhuangLeisure', 'true', '麻将商家模式是否庄闲');
INSERT INTO `mahjongconfig` VALUES ('ZZ7PairsAddIntegral', '1', '转转麻将小七对增加基础分');
INSERT INTO `mahjongconfig` VALUES ('ZZConcealedKongIntegral', '2', '转转麻将暗杠基础分');
INSERT INTO `mahjongconfig` VALUES ('ZZExposedKongIntegral', '1', '转转麻将明杠基础分');
INSERT INTO `mahjongconfig` VALUES ('ZZWinFangBlastIntegral', '1', '转转麻将放炮基础分');
INSERT INTO `mahjongconfig` VALUES ('ZZWinMyselfIntegral', '2', '转转麻将自摸基础分');

-- ----------------------------
-- Table structure for `mahjonghouse`
-- ----------------------------
DROP TABLE IF EXISTS `mahjonghouse`;
CREATE TABLE `mahjonghouse` (
  `houseId` bigint(64) unsigned NOT NULL,
  `houseCardId` int(32) NOT NULL,
  `logicId` int(32) NOT NULL,
  `currentBureau` int(32) NOT NULL,
  `maxBureau` int(32) NOT NULL,
  `maxPlayerNum` int(32) NOT NULL,
  `curPlayerNum` int(32) NOT NULL,
  `businessId` int(32) NOT NULL,
  `housePropertyType` int(32) NOT NULL,
  `catchBird` int(32) NOT NULL,
  `flutter` int(32) NOT NULL,
  `houseType` tinyint(8) NOT NULL,
  `mahjongType` tinyint(8) NOT NULL,
  `houseStatus` tinyint(8) NOT NULL,
  `createTime` datetime NOT NULL,
  `endTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of mahjonghouse
-- ----------------------------

-- ----------------------------
-- Table structure for `mahjongplayer`
-- ----------------------------
DROP TABLE IF EXISTS `mahjongplayer`;
CREATE TABLE `mahjongplayer` (
  `houseId` bigint(64) unsigned NOT NULL,
  `summonerId` bigint(64) unsigned NOT NULL,
  `playerIndex` int(32) NOT NULL,
  `zhuangLeisureType` tinyint(8) NOT NULL,
  `bGetRecord` tinyint(8) NOT NULL,
  `smallWinFangBlast` int(32) NOT NULL,
  `bigWinFangBlast` int(32) NOT NULL,
  `smallWinJieBlast` int(32) NOT NULL,
  `bigWinJieBlast` int(32) NOT NULL,
  `smallWinMyself` int(32) NOT NULL,
  `bigWinMyself` int(32) NOT NULL,
  `allIntegral` int(32) NOT NULL,
  PRIMARY KEY (`houseId`,`summonerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of mahjongplayer
-- ----------------------------

-- ----------------------------
-- Table structure for `payconfig`
-- ----------------------------
DROP TABLE IF EXISTS `payconfig`;
CREATE TABLE `payconfig` (
  `productId` varchar(32) NOT NULL,
  `enable` tinyint(8) NOT NULL,
  `fee` int(32) NOT NULL,
  `gainRoomCard` int(32) NOT NULL,
  `rewardRoomCard` int(32) NOT NULL,
  `remark` varchar(32) NOT NULL,
  PRIMARY KEY (`productId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of payconfig
-- ----------------------------
INSERT INTO `payconfig` VALUES ('cs_mahjong_agent_280', '1', '280', '240', '0', '280元获得240张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_agent_500', '1', '500', '480', '0', '500元获得480张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_agent_900', '1', '900', '900', '0', '900元获得900张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_player_120', '1', '120', '40', '80', '120元充40张送80张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_player_15', '1', '15', '5', '0', '15元获得5张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_player_30', '1', '30', '10', '15', '30元充10张送15张房卡');
INSERT INTO `payconfig` VALUES ('cs_mahjong_player_90', '1', '90', '30', '50', '90元充30张送50张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_agent_280', '1', '280', '240', '0', '280元获得240张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_agent_500', '1', '500', '480', '0', '500元获得480张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_agent_900', '1', '900', '900', '0', '900元获得900张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_player_120', '1', '120', '40', '80', '120元充40张送80张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_player_15', '1', '15', '5', '0', '15元获得5张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_player_30', '1', '30', '10', '15', '30元充10张送15张房卡');
INSERT INTO `payconfig` VALUES ('xs_mahjong_player_90', '1', '90', '30', '50', '90元充30张送50张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_agent_280', '1', '280', '240', '0', '280元获得240张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_agent_500', '1', '500', '480', '0', '500元获得480张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_agent_900', '1', '900', '900', '0', '900元获得900张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_player_120', '1', '120', '40', '80', '120元充40张送80张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_player_15', '1', '15', '5', '0', '15元获得5张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_player_30', '1', '30', '10', '15', '30元充10张送15张房卡');
INSERT INTO `payconfig` VALUES ('yy_mahjong_player_90', '1', '90', '30', '50', '90元充30张送50张房卡');

-- ----------------------------
-- Table structure for `payorder`
-- ----------------------------
DROP TABLE IF EXISTS `payorder`;
CREATE TABLE `payorder` (
  `guid` bigint(64) NOT NULL DEFAULT '0',
  `transactionId` varchar(32) NOT NULL,
  `userId` varchar(64) NOT NULL,
  `summonerId` bigint(64) unsigned NOT NULL,
  `nickName` varchar(32) NOT NULL,
  `productId` varchar(32) NOT NULL,
  `totalFee` double(64,2) NOT NULL,
  `receivegoodstime` datetime NOT NULL,
  `buytime` datetime NOT NULL,
  `state` tinyint(8) NOT NULL,
  `type` varchar(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of payorder
-- ----------------------------

-- ----------------------------
-- Table structure for `profiler`
-- ----------------------------
DROP TABLE IF EXISTS `profiler`;
CREATE TABLE `profiler` (
  `serverName` varchar(32) NOT NULL,
  `serverID` int(32) NOT NULL,
  `name` varchar(128) NOT NULL,
  `timeElapsed` bigint(64) NOT NULL,
  `cpuCycles` bigint(64) NOT NULL,
  `gcGeneration` varchar(256) NOT NULL,
  `callCount` bigint(64) NOT NULL,
  `msgSize` int(32) NOT NULL,
  PRIMARY KEY (`serverName`,`serverID`,`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of profiler
-- ----------------------------

-- ----------------------------
-- Table structure for `rechargedetail`
-- ----------------------------
DROP TABLE IF EXISTS `rechargedetail`;
CREATE TABLE `rechargedetail` (
  `rechargeId` bigint(64) unsigned NOT NULL DEFAULT '0',
  `seller` varchar(32) NOT NULL,
  `target` varchar(32) NOT NULL,
  `roomCard` int(32) NOT NULL,
  `isGift` tinyint(8) unsigned NOT NULL DEFAULT '0',
  `time` datetime NOT NULL,
  `reason` varchar(128) NOT NULL,
  `sellerAuth` int(32) NOT NULL DEFAULT '0',
  `targetIsPlayer` tinyint(8) unsigned NOT NULL DEFAULT '0',
  `amount` int(32) DEFAULT '0',
  PRIMARY KEY (`rechargeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of rechargedetail
-- ----------------------------

-- ----------------------------
-- Table structure for `recordbusinessuser`
-- ----------------------------
DROP TABLE IF EXISTS `recordbusinessuser`;
CREATE TABLE `recordbusinessuser` (
  `businessId` int(32) NOT NULL,
  `recordTime` datetime NOT NULL,
  `allUsers` int(32) NOT NULL,
  `newUsers` int(32) unsigned NOT NULL,
  `effectiveUsers` int(32) NOT NULL,
  `useOneTickets` int(32) NOT NULL,
  `useTwoTickets` int(32) NOT NULL,
  `useThreeTickets` int(32) NOT NULL,
  `useFourTickets` int(32) NOT NULL,
  PRIMARY KEY (`businessId`,`recordTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of recordbusinessuser
-- ----------------------------

-- ----------------------------
-- Table structure for `recordloginuser`
-- ----------------------------
DROP TABLE IF EXISTS `recordloginuser`;
CREATE TABLE `recordloginuser` (
  `recordTime` datetime NOT NULL,
  `newUsers` int(32) unsigned NOT NULL,
  `loginUsers` int(32) NOT NULL,
  PRIMARY KEY (`recordTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of recordloginuser
-- ----------------------------

-- ----------------------------
-- Table structure for `recordroomcard`
-- ----------------------------
DROP TABLE IF EXISTS `recordroomcard`;
CREATE TABLE `recordroomcard` (
  `recordID` bigint(64) unsigned NOT NULL,
  `summonerId` bigint(64) unsigned NOT NULL,
  `recordRoomCardType` tinyint(8) NOT NULL,
  `roomCard` int(32) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`recordID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of recordroomcard
-- ----------------------------

-- ----------------------------
-- Table structure for `roomcard`
-- ----------------------------
DROP TABLE IF EXISTS `roomcard`;
CREATE TABLE `roomcard` (
  `id` bigint(64) unsigned NOT NULL,
  `nickName` varchar(32) NOT NULL,
  `roomCard` int(32) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of roomcard
-- ----------------------------

-- ----------------------------
-- Table structure for `runfastbureau`
-- ----------------------------
DROP TABLE IF EXISTS `runfastbureau`;
CREATE TABLE `runfastbureau` (
  `houseId` bigint(64) unsigned NOT NULL,
  `bureau` bigint(64) unsigned NOT NULL,
  `playerinfo` blob NOT NULL,
  `playercard` blob NOT NULL,
  `showcard` blob NOT NULL,
  `bureauTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`,`bureau`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of runfastbureau
-- ----------------------------

-- ----------------------------
-- Table structure for `runfastconfig`
-- ----------------------------
DROP TABLE IF EXISTS `runfastconfig`;
CREATE TABLE `runfastconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(128) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of runfastconfig
-- ----------------------------
INSERT INTO `runfastconfig` VALUES ('RunFastBureauByHouseCard', '10,1|20,2', '跑得快开局数对应的房卡消耗');
INSERT INTO `runfastconfig` VALUES ('RunFastCardCount', '16', '跑得快商家模式最大张数');
INSERT INTO `runfastconfig` VALUES ('RunFastMaxBureau', '5', '跑得快商家模式最大对局数');
INSERT INTO `runfastconfig` VALUES ('RunFastMaxPlayerNum', '3', '跑得快商家模式最大人数');
INSERT INTO `runfastconfig` VALUES ('RunFastSpadeThree', 'true', '跑得快商家模式是否第一局必出黑桃三');
INSERT INTO `runfastconfig` VALUES ('RunFastStrongOff', 'true', '跑得快商家模式是否判断强关');
INSERT INTO `runfastconfig` VALUES ('RunFastSurplusCardCount', 'false', '跑得快商家模式是否显示剩余牌数');
INSERT INTO `runfastconfig` VALUES ('RunFastZhaDanLoseIntegral', '10', '跑得快炸弹输得人要扣的积分');

-- ----------------------------
-- Table structure for `runfasthouse`
-- ----------------------------
DROP TABLE IF EXISTS `runfasthouse`;
CREATE TABLE `runfasthouse` (
  `houseId` bigint(64) unsigned NOT NULL,
  `houseCardId` int(32) NOT NULL,
  `logicId` int(32) NOT NULL,
  `currentBureau` int(32) NOT NULL,
  `maxBureau` int(32) NOT NULL,
  `curPlayerNum` int(32) NOT NULL,
  `maxPlayerNum` int(32) NOT NULL,
  `businessId` int(32) NOT NULL,
  `zhuangPlayerIndex` int(32) NOT NULL,
  `housePropertyType` int(32) NOT NULL,
  `houseType` tinyint(8) NOT NULL,
  `runFastType` tinyint(8) NOT NULL,
  `houseStatus` tinyint(8) NOT NULL,
  `createTime` datetime NOT NULL,
  `endTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of runfasthouse
-- ----------------------------

-- ----------------------------
-- Table structure for `runfastplayer`
-- ----------------------------
DROP TABLE IF EXISTS `runfastplayer`;
CREATE TABLE `runfastplayer` (
  `houseId` bigint(64) unsigned NOT NULL,
  `summonerId` bigint(64) unsigned NOT NULL,
  `playerIndex` int(32) NOT NULL,
  `bGetRecord` tinyint(8) NOT NULL,
  `bombIntegral` int(32) NOT NULL,
  `winBureau` int(32) NOT NULL,
  `loseBureau` int(32) NOT NULL,
  `allIntegral` int(32) NOT NULL,
  PRIMARY KEY (`houseId`,`summonerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of runfastplayer
-- ----------------------------

-- ----------------------------
-- Table structure for `serverconfig`
-- ----------------------------
DROP TABLE IF EXISTS `serverconfig`;
CREATE TABLE `serverconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(128) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of serverconfig
-- ----------------------------
INSERT INTO `serverconfig` VALUES ('bindCodeRewardRoomCard', '5', '绑定邀请码的奖励房卡数');
INSERT INTO `serverconfig` VALUES ('BusinessDissolveVoteTime', '60', '活动房间解散计时时间(秒)');
INSERT INTO `serverconfig` VALUES ('DailySharingRewardHouseCard', '1', '每日分享奖励房卡数(张)');
INSERT INTO `serverconfig` VALUES ('feedbackPhoneNumberSizeLimit', '20', '反馈功能手机长度限制');
INSERT INTO `serverconfig` VALUES ('feedbackTextSizeLimit', '200', '反馈功能正文长度限制');
INSERT INTO `serverconfig` VALUES ('GetTheOverallRecordNumber', '10', '获取大局战绩局数');
INSERT INTO `serverconfig` VALUES ('GetTheOverallRecordTime', '2', '获取大局战绩时间(天)');
INSERT INTO `serverconfig` VALUES ('HouseDissolveVoteTime', '180', '房间解散计时时间(秒)');
INSERT INTO `serverconfig` VALUES ('InitAnnouncement', '哈哈，我不是傻瓜！', '开服初始公告');
INSERT INTO `serverconfig` VALUES ('InitRoomCard', '5', '初始值房卡');
INSERT INTO `serverconfig` VALUES ('IsOpenCreateHouse', 'true', '是否开启创建房间');
INSERT INTO `serverconfig` VALUES ('IsOpenDelHouseCard', 'false', '是否开启扣房卡模式');
INSERT INTO `serverconfig` VALUES ('MsgSizeLimit', '255', '聊天内容大小限制');
INSERT INTO `serverconfig` VALUES ('RoomInformationRetentionTime', '3', '房间信息保存时间(天)');
INSERT INTO `serverconfig` VALUES ('sendFeedbackMsgCD', '300', '反馈功能发送时间限制(秒)');
INSERT INTO `serverconfig` VALUES ('SendMsgCD', '3', '聊天CD(秒)');
INSERT INTO `serverconfig` VALUES ('ZombieHouseRetentionTime', '30', '僵尸房间保存时间(天)');

-- ----------------------------
-- Table structure for `serverdeployconfig`
-- ----------------------------
DROP TABLE IF EXISTS `serverdeployconfig`;
CREATE TABLE `serverdeployconfig` (
  `name` varchar(128) NOT NULL,
  `id` int(32) NOT NULL,
  `ip` varchar(128) NOT NULL,
  `tcp_port` int(32) NOT NULL,
  `remark` varchar(256) NOT NULL,
  PRIMARY KEY (`name`,`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of serverdeployconfig
-- ----------------------------
INSERT INTO `serverdeployconfig` VALUES ('ac', '1', '192.168.0.10', '9000', '认证服务器1');
INSERT INTO `serverdeployconfig` VALUES ('ac', '2', '192.168.0.10', '9001', '认证服务器2');
INSERT INTO `serverdeployconfig` VALUES ('center', '1', '192.168.0.10', '7000', '中心服务器');
INSERT INTO `serverdeployconfig` VALUES ('db', '1', '192.168.0.10', '7500', '数据服务器1');
INSERT INTO `serverdeployconfig` VALUES ('db', '2', '192.168.0.10', '7501', '数据服务器2');
INSERT INTO `serverdeployconfig` VALUES ('logic', '1', '192.168.0.10', '8000', '逻辑服务器1');
INSERT INTO `serverdeployconfig` VALUES ('logic', '2', '192.168.0.10', '8001', '逻辑服务器2');
INSERT INTO `serverdeployconfig` VALUES ('proxy', '1', '192.168.0.10', '8500', '代理服务器1');
INSERT INTO `serverdeployconfig` VALUES ('proxy', '2', '192.168.0.10', '8501', '代理服务器2');
INSERT INTO `serverdeployconfig` VALUES ('record', '1', '192.168.0.10', '7002', '日志服务器');
INSERT INTO `serverdeployconfig` VALUES ('world', '1', '192.168.0.10', '7001', '世界服务器1');

-- ----------------------------
-- Table structure for `serviceboxtest`
-- ----------------------------
DROP TABLE IF EXISTS `serviceboxtest`;
CREATE TABLE `serviceboxtest` (
  `guid` bigint(64) unsigned NOT NULL,
  `param1` int(32) NOT NULL,
  `param2` int(32) NOT NULL,
  `param3` int(32) NOT NULL,
  `param4` float(32,0) NOT NULL,
  `param5` float(32,0) NOT NULL,
  `param6` float(32,0) NOT NULL,
  `param7` varchar(64) NOT NULL,
  `param8` varchar(64) NOT NULL,
  `param9` varchar(64) NOT NULL,
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of serviceboxtest
-- ----------------------------

-- ----------------------------
-- Table structure for `shopinfo`
-- ----------------------------
DROP TABLE IF EXISTS `shopinfo`;
CREATE TABLE `shopinfo` (
  `name` varchar(32) NOT NULL,
  `shopkeeper` varchar(32) NOT NULL DEFAULT '',
  `area` varchar(32) NOT NULL,
  `tel` varchar(32) NOT NULL DEFAULT '0',
  `address` varchar(128) NOT NULL,
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of shopinfo
-- ----------------------------

-- ----------------------------
-- Table structure for `specialactivitiesconfig`
-- ----------------------------
DROP TABLE IF EXISTS `specialactivitiesconfig`;
CREATE TABLE `specialactivitiesconfig` (
  `ID` int(32) NOT NULL,
  `Name` varchar(32) NOT NULL,
  `RuleInfo` varchar(512) NOT NULL,
  `Intro` varchar(512) NOT NULL,
  `OrderId` int(32) NOT NULL,
  `TitleLogoURL` varchar(128) NOT NULL,
  `MainLogoURL` varchar(128) NOT NULL,
  `MainPicURL` varchar(128) NOT NULL,
  `BroadcastPicURL` varchar(128) NOT NULL,
  `AdminId` varchar(128) NOT NULL,
  `Tickets` varchar(128) NOT NULL,
  `DiscountInfo` varchar(256) NOT NULL,
  `BusinessLicense` varchar(128) NOT NULL,
  `ContactAddress` varchar(128) NOT NULL,
  `ContactCall` varchar(128) NOT NULL,
  `ContactPerson` varchar(128) NOT NULL,
  `DynamicPasswordIndate` int(32) NOT NULL,
  `UpdateFlag` int(32) NOT NULL,
  `ComTickets` varchar(128) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='专场ID,专场名称,游戏规则,简介,序号,标题LogoURL,主LogoURL,主图URL,宣传图URL,管理员ID,券列表,专场折扣描述,营业执照,联系地址,联系电话,联系人,动态口令生存期,版本号\r\nID,Name,RuleInfo,Intro,OrderId,TitleLogoURL,MainLogoURL,MainPicURL,AdminId,BroadcastPicURL,Tickets,DiscountInfo,UpdateFlag';

-- ----------------------------
-- Records of specialactivitiesconfig
-- ----------------------------
INSERT INTO `specialactivitiesconfig` VALUES ('2', '邀朋晋级赛', '活动规则：\\n1.输入动态码,点击下方“加入游戏”，系统自动匹配，额满开赛\\n2.每轮8局，按积分排名，第一轮决出16强，第二轮16进4，第三轮4强决胜\\n3.比赛结束，即时公布名次，待官方审核颁奖', '邀朋晋级赛 \\n\\n电话：0731-84442999', '1', 'http://yyres.jiayehuyu.com/WordPlate/Markets/2/1.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/2/2.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/2/3.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/2/4.png', '10000;10001;10106', '', '1.每个优惠券使用仅限一次\\n2.活动时间内可凭券索取红包\\n3.活动时间：2016.11.24-2016.11.27\\n4.本活动最终解释权归邀朋游戏决定', 'BT998777678ABD', '湖南嘉业互娱网络有限公司', '0731-94442999', '法人', '180', '1', '');
INSERT INTO `specialactivitiesconfig` VALUES ('3', '邀朋美事晋级赛', '活动规则：\\n1.留意DJ公布比赛动态码，输入动态码点击下方“加入游戏”，系统自动匹配，额满开赛\\n2.每轮5局，按积分排名，第一轮决出16强，第二轮16进4，第三轮4强决胜', '邀朋美事晋级赛 \\n\\n电话：0731-87792772', '1', 'http://yyres.jiayehuyu.com/WordPlate/Markets/3/1.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/3/2.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/3/3.png', 'http://yyres.jiayehuyu.com/WordPlate/Markets/3/4.png', '10000;10001;10106', ' ', '1.每个优惠券使用仅限一次\\n2.活动时间内可凭券索取红包\\n3.活动时间：2016.11.24-2016.11.27\\n4.本活动最终解释权归邀朋游戏决定', 'BT998777678ABD', '湖南嘉业互娱网络有限公司', '0731-94442999', '法人', '180', '1', '');

-- ----------------------------
-- Table structure for `summoner`
-- ----------------------------
DROP TABLE IF EXISTS `summoner`;
CREATE TABLE `summoner` (
  `id` bigint(64) unsigned NOT NULL,
  `userId` varchar(64) NOT NULL,
  `sex` tinyint(8) NOT NULL,
  `nickName` varchar(32) NOT NULL,
  `loginTime` datetime NOT NULL,
  `registTime` datetime NOT NULL,
  `auth` tinyint(8) NOT NULL DEFAULT '1',
  `unLockTime` datetime NOT NULL,
  `houseId` bigint(64) unsigned NOT NULL,
  `competitionKey` int(32) NOT NULL,
  `allIntegral` int(32) NOT NULL,
  `headImgUrl` varchar(255) NOT NULL,
  `dailySharingTime` datetime NOT NULL,
  `bOpenHouse` tinyint(8) NOT NULL,
  `business` blob NOT NULL,
  `tickets` blob NOT NULL,
  `belong` bigint(64) unsigned NOT NULL,
  `belongBindTime` datetime NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of summoner
-- ----------------------------

-- ----------------------------
-- Table structure for `summonerblacklist`
-- ----------------------------
DROP TABLE IF EXISTS `summonerblacklist`;
CREATE TABLE `summonerblacklist` (
  `summonerId` bigint(64) unsigned NOT NULL,
  PRIMARY KEY (`summonerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of summonerblacklist
-- ----------------------------

-- ----------------------------
-- Table structure for `systemconfig`
-- ----------------------------
DROP TABLE IF EXISTS `systemconfig`;
CREATE TABLE `systemconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(1024) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of systemconfig
-- ----------------------------
INSERT INTO `systemconfig` VALUES ('AliPayAccount', 'baiyougame@163.com', '支付宝帐户');
INSERT INTO `systemconfig` VALUES ('AliPayNotifyURL', 'http://113.240.93.249:81/AliPayNotify_url.aspx', '支付宝支付回调webserver地址');
INSERT INTO `systemconfig` VALUES ('AliPayPID', '2088421326597412', '支付宝商户ID');
INSERT INTO `systemconfig` VALUES ('AliPayPrivateKey', 'MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAMKk1fCwDO2+0+Vs3W3jmtZFb4q8q+pWdERv3r8WXZxYHOLVB3IWsIDGVxLsr1tFfeIItCj3E46y+nQO4KwHIPONkevq6SfzFZk9Rpv3KxXF8gNgvKNzEKLXkuANxZvMKN+FgrYwsMJal/1FsFiubPRd+HcgevPLjufUo0KBHRC9AgMBAAECgYADge7tCG8jNaYh40VWPpzCsbuh12aNsYMk3JM6BFApihjzKX2Z27jQiUJ2b9d1IJp1IU0F0/YBQ05qGv6HexVn5g9A7SyKOW8SMmQ+3xBRFDV3ltQCTIunBpAfW/RjL2CLENhJIdFy3jIrxU5IR0PTx28EAnGXqWxeqbitKpy4YQJBAP4msoU19CJFvZvzOxixTzCwCPu4Adfr2cvpjhll7bCzGHRRbAe1cZRywQU0oOph5PtpA00stXlcuMOXxd/NiqUCQQDED1GVEg8wH+ynhBf4A2QOvql6IaZ4sEIrtejOfqW0Vq8jipofwGxhV1C9wx+xz9+j2ex21UP0HOwQ+KcNt8o5AkAC1LeEWFeB0jkMdacg3Ui+iBdxhlku6Ieih9V3XvVI1JXfJdEIPPMo7iZMQovQUIrWahMJVwgmc+vy8cvYdFepAkB8WG7YibyiPA0e7VM6VAQ4qcnZENCBCODe0h+WH5K+vp+TfgtkCelzDyrBP6ixYHKpe8RSVc4kW9eIp/tjYuZZAkBzuLwiVMY8xTyjmt+akz9MJprZW5UpxEUXJHmu54JHvLsd2GCXrzdlKIx/5VfINdjma6zirqIRYL7tRgTyLNSB', '支付宝私钥');
INSERT INTO `systemconfig` VALUES ('AliPayPublicKey', 'MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCnxj/9qwVfgoUh/y2W89L6BkRAFljhNhgPdyPuBV64bfQNN1PjbCzkIM6qRdKBoLPXmKKMiFYnkd6rAoprih3/PrQEB/VsW8OoM8fxn67UDYuyBTqA23MML9q1+ilIZwBC2AQ2UBVOrFXfFl75p6/B5KsiNG9zpgmLCUYuLkxpLQIDAQAB', '支付宝公钥');
INSERT INTO `systemconfig` VALUES ('checkAppStoreReceiptBetaURL', 'https://sandbox.itunes.apple.com/verifyReceipt', '苹果支付沙盒测试环境下的交易凭据验证网址');
INSERT INTO `systemconfig` VALUES ('checkAppStoreReceiptReleaseURL', 'https://buy.itunes.apple.com/verifyReceipt', '苹果支付生产环境下的交易凭据验证网址');
INSERT INTO `systemconfig` VALUES ('defaultHeadIconURL', 'http://jiayehuyu.com/GameIcon/DefaultHeadIcon.png', '默认头像');
INSERT INTO `systemconfig` VALUES ('facebookUserInfoURL', 'https://graph.facebook.com/v2.5/me?', 'Facebook取用户信息的网址');
INSERT INTO `systemconfig` VALUES ('googlePayVerifyPublicKey', 'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuzKAmfHBwpfDwpJhDMeyqKmIFPjSt3mAlkAUDxBhLBCXppHquRtVoVmkEaheA0SX1sXJSb5mZuQFyJovWnKJTNjmY4bU+QVDZfNcSQwt/qZX5BXEsuhnUrF5VRfGPxV5vP/nav62k/PLyDEyMHTtEnBfLDiZgaccwgSqtNE3PriT2Q+jXusVCyW6nI+/iSquUy8wxtXfNe+NXNZzAobLLfNOiK9RDLhuhZrQOfkzxEaqySMlArH1bbA1uQdwgJ/3bNI4ZoUq9AsVpkVtwLYxCGbSNw/BfUWRvLz/QIb3uuzF8wQ8WRn0SHD3ux7UrAgSDq8ShmC19U/8eLhxrzeEyQIDAQAB', '谷歌支付验证所需公钥');
INSERT INTO `systemconfig` VALUES ('googlePlayUserInfoURL', 'https://www.googleapis.com/oauth2/v2/userinfo?', 'GooglePlay取用户信息的网址');
INSERT INTO `systemconfig` VALUES ('MQHostName', 'localhost', '消息队列主机地址');
INSERT INTO `systemconfig` VALUES ('MQUserName', 'root', '消息队列用户名');
INSERT INTO `systemconfig` VALUES ('MQUserPassword', 'root', '消息队列密码');
INSERT INTO `systemconfig` VALUES ('openAccessTokenVerify', 'true', '是否开启登陆token的服务器验证，生产环境中必须设置为true，后台工具做压测时可以置false');
INSERT INTO `systemconfig` VALUES ('openBeginLogin', 'true', '是否开启登陆');
INSERT INTO `systemconfig` VALUES ('openBoxAutoPlaying', 'false', '是否开启盒子自动打牌');
INSERT INTO `systemconfig` VALUES ('openBuyingDirect', 'true', '是否开启本地直接购买接口(包括钻石充值,VIP,理财卡,限时购买礼包)');
INSERT INTO `systemconfig` VALUES ('openDebug', 'true', '是否处理调试日志');
INSERT INTO `systemconfig` VALUES ('openGM', 'true', '是否开启GM');
INSERT INTO `systemconfig` VALUES ('openRankReward', 'true', '是否开启普通排行榜每日发放奖励');
INSERT INTO `systemconfig` VALUES ('openRootLogin', 'true', '是否开启root帐号登陆');
INSERT INTO `systemconfig` VALUES ('openTransaction', 'true', '是否打开支付交易');
INSERT INTO `systemconfig` VALUES ('procMsgAttackCheckLockTime', '3', '对恶意攻击的玩家的锁定时间秒');
INSERT INTO `systemconfig` VALUES ('procMsgAttackCheckTimePeriod', '3000', '频繁攻击消息的检测时间周期ms，为了防治网络拥堵造成的突然1秒或2秒内处理超过50个包的问题而造成误判，所以设置为3秒');
INSERT INTO `systemconfig` VALUES ('procMsgAttackWarningCntLimit', '3', '对恶意攻击的玩家的警告的次数，超过3次则将被强制断开连接');
INSERT INTO `systemconfig` VALUES ('procMsgCountLimitAttackCheck', '50', '基于procMsgAttackCheckTimePeriod的时间内能处理的消息个数限制,　3秒内处理50个包相当于1000的APM，是世界人手速最快记录的2倍');
INSERT INTO `systemconfig` VALUES ('tokenExistTime', '60', 'token在网关的保留时间（分钟）');
INSERT INTO `systemconfig` VALUES ('userFieldsByTokenCheck', 'id', '从第三方取出来的用户信息中作用token认证的字段名称');
INSERT INTO `systemconfig` VALUES ('yiJieAppID', '34C7DEE23F22F72B', 'CP游戏在易接服务器上的ID。由易接提供。');
INSERT INTO `systemconfig` VALUES ('yiJieLoginCheckURL', 'http://sync.1sdk.cn/login/check.html', '国内易接登陆验证地址');
INSERT INTO `systemconfig` VALUES ('yiJiePayNotifyURL', 'http://113.246.48.140:81/YiJiePayNotify_url.aspx', '用来提供给易接支付回调的地址');
INSERT INTO `systemconfig` VALUES ('yiJiePrivateKey', '9WIN3WB3EEP7GUHVS5EGVHKAL4KM9BSC', '国内易接支付密匙');

-- ----------------------------
-- Table structure for `ticketsconfig`
-- ----------------------------
DROP TABLE IF EXISTS `ticketsconfig`;
CREATE TABLE `ticketsconfig` (
  `ID` int(32) NOT NULL,
  `MarketID` int(32) NOT NULL,
  `Name` varchar(32) NOT NULL,
  `Context` varchar(128) NOT NULL,
  `Indate` int(32) NOT NULL,
  `IconURL` varchar(128) NOT NULL,
  `SettlementInfo` varchar(256) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='券ID,所属商场ID,名称,详细描述,生存期,IconURL,结算描述,,,,,\r\nID,MarketID,Name,Context,Indate,IconURL,SettlementInfo,,,,,';

-- ----------------------------
-- Records of ticketsconfig
-- ----------------------------
INSERT INTO `ticketsconfig` VALUES ('1', '1', '蛙来哒28元菜品', '活动截止11月27日前使用', '2880', 'http://res.jiayehuyu.com/WordPlate/Markets/1/5.png', '价值28元菜品');

-- ----------------------------
-- Table structure for `webconfig`
-- ----------------------------
DROP TABLE IF EXISTS `webconfig`;
CREATE TABLE `webconfig` (
  `configKey` varchar(128) NOT NULL,
  `configValue` varchar(1024) NOT NULL,
  `configRemark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`configKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of webconfig
-- ----------------------------
INSERT INTO `webconfig` VALUES ('GameName', 'YPXSMJ', '游戏的名称（与LocalConfig.csv里对应）');
INSERT INTO `webconfig` VALUES ('isAllowWebLogin', 'true', '是否允许web网页登录，true表示允许，false表示只允许公众号进入');
INSERT INTO `webconfig` VALUES ('MQHostName', 'localhost', '消息队列主机地址');
INSERT INTO `webconfig` VALUES ('MQUserName', 'root', '消息队列主机用户名');
INSERT INTO `webconfig` VALUES ('MQUserPassword', 'root', '消息队列主机密码');
INSERT INTO `webconfig` VALUES ('OpenRegistEmployee', 'true', '客户管理系统是否开启注册');
INSERT INTO `webconfig` VALUES ('OpenRegistGM', 'true', '销售系统是否开启注册');
INSERT INTO `webconfig` VALUES ('SetDefaultSaleMan', '蒋满群', '设置默认业务员');
INSERT INTO `webconfig` VALUES ('setShopkeeperType', 'true', '是否启用代理分类，一级、二级代理');

-- ----------------------------
-- Table structure for `wordplatebureau`
-- ----------------------------
DROP TABLE IF EXISTS `wordplatebureau`;
CREATE TABLE `wordplatebureau` (
  `houseId` bigint(64) unsigned NOT NULL,
  `bureau` bigint(64) unsigned NOT NULL,
  `playerinfo` blob NOT NULL,
  `playerWordPlate` blob NOT NULL,
  `showWordPlate` blob NOT NULL,
  `bureauTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`,`bureau`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of wordplatebureau
-- ----------------------------

-- ----------------------------
-- Table structure for `wordplateconfig`
-- ----------------------------
DROP TABLE IF EXISTS `wordplateconfig`;
CREATE TABLE `wordplateconfig` (
  `key` varchar(128) NOT NULL,
  `value` varchar(128) NOT NULL,
  `remark` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records of wordplateconfig
-- ----------------------------
INSERT INTO `wordplateconfig` VALUES ('WHZWordPlateBasaWinScore', '6,7', '歪胡子对应的胡牌基础积分');
INSERT INTO `wordplateconfig` VALUES ('WHZWordPlateMaxWinScore', '200,300,400', '歪胡子对应的胡牌最大积分');
INSERT INTO `wordplateconfig` VALUES ('WordPlateBaseWinScore', '7', '字牌商家基础胡息(6,7)');
INSERT INTO `wordplateconfig` VALUES ('WordPlateBureauByHouseCard', '10,1|20,2', '字牌开局数对应的房卡消耗');
INSERT INTO `wordplateconfig` VALUES ('WordPlateIsOpenBaoTing', 'true', '字牌商家是否计算报听');
INSERT INTO `wordplateconfig` VALUES ('WordPlateIsOpenBigSmallHu', 'true', '字牌商家是否计算大小胡');
INSERT INTO `wordplateconfig` VALUES ('WordPlateIsOpenFamous', 'true', '字牌商家是否计算名堂');
INSERT INTO `wordplateconfig` VALUES ('WordPlateMaxBureau', '4', '字牌商家模式最大对局数');
INSERT INTO `wordplateconfig` VALUES ('WordPlateMaxWinScore', '400', '字牌商家最大胡息(200,300,400)');
INSERT INTO `wordplateconfig` VALUES ('WordPlateType', '1', '字牌商家模式麻将类型(1 歪胡子)');

-- ----------------------------
-- Table structure for `wordplatehouse`
-- ----------------------------
DROP TABLE IF EXISTS `wordplatehouse`;
CREATE TABLE `wordplatehouse` (
  `houseId` bigint(64) unsigned NOT NULL,
  `houseCardId` int(32) NOT NULL,
  `logicId` int(32) NOT NULL,
  `currentBureau` int(32) NOT NULL,
  `maxBureau` int(32) NOT NULL,
  `maxWinScore` int(32) NOT NULL,
  `curPlayerNum` int(32) NOT NULL,
  `maxPlayerNum` int(32) NOT NULL,
  `businessId` int(32) NOT NULL,
  `housePropertyType` int(32) NOT NULL,
  `baseWinScore` int(32) NOT NULL,
  `beginGodType` int(32) NOT NULL,
  `houseType` tinyint(8) NOT NULL,
  `wordPlateType` tinyint(8) NOT NULL,
  `houseStatus` tinyint(8) NOT NULL,
  `createTime` datetime NOT NULL,
  `endTime` datetime NOT NULL,
  PRIMARY KEY (`houseId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of wordplatehouse
-- ----------------------------

-- ----------------------------
-- Table structure for `wordplateplayer`
-- ----------------------------
DROP TABLE IF EXISTS `wordplateplayer`;
CREATE TABLE `wordplateplayer` (
  `houseId` bigint(64) unsigned NOT NULL,
  `summonerId` bigint(64) unsigned NOT NULL,
  `playerIndex` int(32) NOT NULL,
  `zhuangLeisureType` tinyint(8) NOT NULL,
  `bGetRecord` tinyint(8) NOT NULL,
  `winAmount` int(32) NOT NULL,
  `allWinScore` int(32) NOT NULL,
  `allIntegral` int(32) NOT NULL,
  PRIMARY KEY (`houseId`,`summonerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of wordplateplayer
-- ----------------------------
