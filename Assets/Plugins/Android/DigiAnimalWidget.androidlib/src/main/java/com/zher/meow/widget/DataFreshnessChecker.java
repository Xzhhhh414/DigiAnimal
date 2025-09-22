package com.zher.meow.widget;

import android.util.Log;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * 数据新鲜度检查器
 * 负责判断游戏数据和离线数据哪个更新鲜
 */
public class DataFreshnessChecker {
    private static final String TAG = "DataFreshnessChecker";
    
    /**
     * 判断游戏数据是否比离线数据更新鲜
     * 比较规则：游戏存档的lastUpdateTime > 离线数据的lastCalculationTime
     */
    public static boolean isGameDataFresher(PetData gameData, OfflineDataManager.OfflineBaseData offlineData) {
        if (gameData == null) {
            // Log.d(TAG, "游戏数据为空，不新鲜");
            return false;
        }
        
        if (offlineData == null) {
            // Log.d(TAG, "离线数据为空，游戏数据更新鲜");
            return true;
        }
        
        // 解析游戏数据的时间戳
        long gameTimestamp = parseTimestamp(gameData.lastUpdateTime);
        long offlineTimestamp = offlineData.lastCalculationTime;
        
        boolean isGameFresher = gameTimestamp > offlineTimestamp;
        
        // Log.d(TAG, "数据新鲜度比较: " +
        //       "游戏时间戳=" + gameTimestamp + " (" + gameData.lastUpdateTime + "), " +
        //       "离线时间戳=" + offlineTimestamp + ", " +
        //       "游戏数据更新鲜=" + isGameFresher);
        
        return isGameFresher;
    }
    
    /**
     * 比较两个时间戳
     * @param timestamp1 第一个时间戳
     * @param timestamp2 第二个时间戳
     * @return timestamp1 > timestamp2 返回true
     */
    public static boolean compareTimestamps(long timestamp1, long timestamp2) {
        return timestamp1 > timestamp2;
    }
    
    /**
     * 决定应该使用哪个数据源
     * @param gameData 游戏数据
     * @param offlineData 离线数据
     * @return true表示使用游戏数据，false表示使用离线数据
     */
    public static boolean shouldUseGameData(PetData gameData, OfflineDataManager.OfflineBaseData offlineData) {
        // 如果没有游戏数据，使用离线数据
        if (gameData == null) {
            // Log.d(TAG, "没有游戏数据，使用离线数据");
            return false;
        }
        
        // 如果没有离线数据，使用游戏数据
        if (offlineData == null) {
            // Log.d(TAG, "没有离线数据，使用游戏数据");
            return true;
        }
        
        // 比较新鲜度
        boolean useGameData = isGameDataFresher(gameData, offlineData);
        
        // Log.d(TAG, "数据源选择: " + (useGameData ? "游戏数据" : "离线数据"));
        return useGameData;
    }
    
    /**
     * 检查数据是否有效
     */
    public static boolean isDataValid(PetData data) {
        if (data == null) {
            return false;
        }
        
        // 检查必要字段
        if (data.petName == null || data.petName.isEmpty()) {
            Log.w(TAG, "宠物名称无效");
            return false;
        }
        
        if (data.prefabName == null || data.prefabName.isEmpty()) {
            Log.w(TAG, "预制体名称无效");
            return false;
        }
        
        // 检查数值范围
        if (data.energy < 0 || data.energy > 1000) {
            Log.w(TAG, "精力值超出范围: " + data.energy);
            return false;
        }
        
        if (data.satiety < 0 || data.satiety > 1000) {
            Log.w(TAG, "饱食值超出范围: " + data.satiety);
            return false;
        }
        
        if (data.ageInDays < 0 || data.ageInDays > 10000) {
            Log.w(TAG, "年龄超出范围: " + data.ageInDays);
            return false;
        }
        
        return true;
    }
    
    /**
     * 检查离线基准数据是否有效
     */
    public static boolean isOfflineDataValid(OfflineDataManager.OfflineBaseData data) {
        if (data == null) {
            return false;
        }
        
        // 检查时间戳
        if (data.baseTimestamp <= 0 || data.lastCalculationTime <= 0) {
            Log.w(TAG, "离线数据时间戳无效");
            return false;
        }
        
        // 检查基准时间不能在未来
        long currentTime = System.currentTimeMillis();
        if (data.baseTimestamp > currentTime + 60000) { // 允许1分钟的时间误差
            Log.w(TAG, "离线数据基准时间在未来: " + data.baseTimestamp + " > " + currentTime);
            return false;
        }
        
        // 检查数值范围
        if (data.baseEnergy < 0 || data.baseEnergy > 1000) {
            Log.w(TAG, "离线基准精力值超出范围: " + data.baseEnergy);
            return false;
        }
        
        if (data.baseSatiety < 0 || data.baseSatiety > 1000) {
            Log.w(TAG, "离线基准饱食值超出范围: " + data.baseSatiety);
            return false;
        }
        
        return true;
    }
    
    /**
     * 解析时间戳字符串
     * 支持多种格式：纯数字时间戳、日期时间字符串等
     */
    private static long parseTimestamp(String timestampStr) {
        if (timestampStr == null || timestampStr.isEmpty()) {
            Log.w(TAG, "时间戳字符串为空");
            return 0;
        }
        
        try {
            // 尝试直接解析为数字时间戳
            return Long.parseLong(timestampStr);
        } catch (NumberFormatException e) {
            // 如果不是纯数字，尝试解析日期时间格式
            try {
                // 解析 "2025-09-22 11:02:16" 格式
                SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault());
                Date date = sdf.parse(timestampStr);
                if (date != null) {
                    long timestamp = date.getTime();
                    // Log.d(TAG, "成功解析时间戳: " + timestampStr + " -> " + timestamp);
                    return timestamp;
                }
            } catch (Exception ex) {
                Log.w(TAG, "日期格式解析失败: " + timestampStr + ", " + ex.getMessage());
            }
            
            // 如果所有解析都失败，返回当前时间
            Log.w(TAG, "无法解析时间戳字符串: " + timestampStr + "，使用当前时间");
            return System.currentTimeMillis();
        }
    }
    
    /**
     * 获取数据年龄（距离现在多长时间）
     */
    public static long getDataAge(PetData data) {
        if (data == null || data.lastUpdateTime == null) {
            return Long.MAX_VALUE; // 返回最大值表示数据很旧
        }
        
        long dataTimestamp = parseTimestamp(data.lastUpdateTime);
        long currentTime = System.currentTimeMillis();
        
        return currentTime - dataTimestamp;
    }
    
    /**
     * 获取离线数据年龄
     */
    public static long getOfflineDataAge(OfflineDataManager.OfflineBaseData data) {
        if (data == null) {
            return Long.MAX_VALUE;
        }
        
        long currentTime = System.currentTimeMillis();
        return currentTime - data.lastCalculationTime;
    }
}
