package com.zher.meow.widget;

import android.util.Log;

/**
 * 离线计算器
 * 负责根据基准数据和经过时间计算当前宠物状态
 */
public class OfflineCalculator {
    private static final String TAG = "OfflineCalculator";
    
    // 游戏规则常量 (测试配置 - 加速衰减)
    private static final long ENERGY_DECAY_INTERVAL = 648; // 精力每5秒降低1点 (测试用)
    private static final long SATIETY_DECAY_INTERVAL = 432; // 饱食每5秒降低1点 (测试用)
    private static final long BORED_RESET_INTERVAL = 600; // isBored在30秒后重置为false (测试用)
    
    /**
     * 根据基准数据计算当前宠物状态
     */
    public static PetData calculateCurrentStats(OfflineDataManager.OfflineBaseData baseData, long currentTime) {
        if (baseData == null) {
            Log.w(TAG, "基准数据为空，返回默认数据");
            return createDefaultPetData();
        }
        
        // 计算从基准时间到现在经过的秒数
        long elapsedSeconds = (currentTime - baseData.baseTimestamp) / 1000;
        
        // 确保时间不为负数（防止系统时间异常）
        if (elapsedSeconds < 0) {
            Log.w(TAG, "时间异常，经过时间为负数: " + elapsedSeconds + "秒");
            elapsedSeconds = 0;
        }
        
        // 计算当前数值
        int currentEnergy = calculateEnergyDecay(baseData.baseEnergy, elapsedSeconds);
        int currentSatiety = calculateSatietyDecay(baseData.baseSatiety, elapsedSeconds);
        boolean currentIsBored = calculateBoredStatus(baseData.baseIsBored, elapsedSeconds);
        
        // 创建计算结果
        PetData result = new PetData();
        result.petId = baseData.petId;
        result.petName = baseData.petName;
        result.prefabName = baseData.prefabName;
        result.energy = currentEnergy;
        result.satiety = currentSatiety;
        result.isBored = currentIsBored;
        result.purchaseDate = ""; // 离线计算不需要这些字段
        result.ageInDays = 1; // 暂时使用默认值，后续可以根据需要计算
        result.introduction = "可爱的宠物";
        result.lastUpdateTime = String.valueOf(currentTime);
        
        // Log.d(TAG, "离线计算结果: " + result.petName + 
        //       ", 精力=" + currentEnergy + " (基准=" + baseData.baseEnergy + ")" +
        //       ", 饱食=" + currentSatiety + " (基准=" + baseData.baseSatiety + ")" +
        //       ", 无聊=" + currentIsBored + " (基准=" + baseData.baseIsBored + ")" +
        //       ", 经过时间=" + elapsedSeconds + "秒");
        
        return result;
    }
    
    /**
     * 计算精力衰减
     * 每648秒降低1点，最小值为0
     */
    public static int calculateEnergyDecay(int baseEnergy, long elapsedSeconds) {
        int decay = (int) (elapsedSeconds / ENERGY_DECAY_INTERVAL);
        int currentEnergy = baseEnergy - decay;
        return Math.max(0, currentEnergy); // 确保不为负数
    }
    
    /**
     * 计算饱食衰减
     * 每432秒降低1点，最小值为0
     */
    public static int calculateSatietyDecay(int baseSatiety, long elapsedSeconds) {
        int decay = (int) (elapsedSeconds / SATIETY_DECAY_INTERVAL);
        int currentSatiety = baseSatiety - decay;
        return Math.max(0, currentSatiety); // 确保不为负数
    }
    
    /**
     * 计算isBored状态
     * 600秒后重置为false，之后不会重新变为true
     */
    public static boolean calculateBoredStatus(boolean baseIsBored, long elapsedSeconds) {
        if (!baseIsBored) {
            return false; // 如果基准状态就是false，保持false
        }
        
        // 如果基准状态是true，600秒后重置为false
        return elapsedSeconds < BORED_RESET_INTERVAL;
    }
    
    /**
     * 获取离线经过的时间（秒）
     */
    public static long getOfflineElapsedTime(long baseTimestamp, long currentTime) {
        long elapsedMillis = currentTime - baseTimestamp;
        return Math.max(0, elapsedMillis / 1000); // 确保不为负数
    }
    
    /**
     * 创建默认宠物数据（当离线数据不可用时使用）
     */
    private static PetData createDefaultPetData() {
        PetData defaultData = new PetData();
        defaultData.petId = "";
        defaultData.petName = "我的宠物";
        defaultData.prefabName = "Pet_CatBrown";
        defaultData.energy = 100;
        defaultData.satiety = 100;
        defaultData.isBored = false;
        defaultData.purchaseDate = "";
        defaultData.ageInDays = 1;
        defaultData.introduction = "可爱的宠物";
        defaultData.lastUpdateTime = String.valueOf(System.currentTimeMillis());
        
        // Log.d(TAG, "使用默认宠物数据");
        return defaultData;
    }
    
    /**
     * 验证计算结果的合理性
     */
    public static boolean validateCalculatedData(PetData data) {
        if (data == null) {
            return false;
        }
        
        // 检查数值范围
        if (data.energy < 0 || data.energy > 1000) {
            Log.w(TAG, "精力值异常: " + data.energy);
            return false;
        }
        
        if (data.satiety < 0 || data.satiety > 1000) {
            Log.w(TAG, "饱食值异常: " + data.satiety);
            return false;
        }
        
        if (data.petName == null || data.petName.isEmpty()) {
            Log.w(TAG, "宠物名称为空");
            return false;
        }
        
        return true;
    }
    
    /**
     * 获取游戏规则常量（用于调试和测试）
     */
    public static class GameRules {
        public static final long ENERGY_DECAY_INTERVAL_SECONDS = ENERGY_DECAY_INTERVAL;
        public static final long SATIETY_DECAY_INTERVAL_SECONDS = SATIETY_DECAY_INTERVAL;
        public static final long BORED_RESET_INTERVAL_SECONDS = BORED_RESET_INTERVAL;
    }
}
