package com.zher.meow.widget;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

/**
 * 离线数据管理器
 * 负责存储和读取离线计算的基准数据
 */
public class OfflineDataManager {
    private static final String TAG = "OfflineDataManager";
    private static final String PREFS_NAME = "widget_offline_data";
    
    // SharedPreferences键名
    private static final String KEY_BASE_ENERGY = "offline_base_energy";
    private static final String KEY_BASE_SATIETY = "offline_base_satiety";
    private static final String KEY_BASE_IS_BORED = "offline_base_is_bored";
    private static final String KEY_BASE_TIMESTAMP = "offline_base_timestamp";
    private static final String KEY_LAST_CALCULATION_TIME = "offline_last_calculation_time";
    private static final String KEY_PET_ID = "offline_pet_id";
    private static final String KEY_PET_NAME = "offline_pet_name";
    private static final String KEY_PREFAB_NAME = "offline_prefab_name";
    
    private SharedPreferences prefs;
    
    public OfflineDataManager(Context context) {
        prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }
    
    /**
     * 保存离线计算的基准数据
     */
    public void saveOfflineBaseData(PetData petData, long timestamp) {
        try {
            SharedPreferences.Editor editor = prefs.edit();
            editor.putString(KEY_PET_ID, petData.petId);
            editor.putString(KEY_PET_NAME, petData.petName);
            editor.putString(KEY_PREFAB_NAME, petData.prefabName);
            editor.putInt(KEY_BASE_ENERGY, petData.energy);
            editor.putInt(KEY_BASE_SATIETY, petData.satiety);
            editor.putBoolean(KEY_BASE_IS_BORED, petData.isBored);
            editor.putLong(KEY_BASE_TIMESTAMP, timestamp);
            editor.putLong(KEY_LAST_CALCULATION_TIME, timestamp);
            editor.apply();
            
            // Log.d(TAG, "离线基准数据已保存: " + petData.petName + 
            //       ", 精力=" + petData.energy + ", 饱食=" + petData.satiety + 
            //       ", 无聊=" + petData.isBored + ", 时间戳=" + timestamp);
        } catch (Exception e) {
            Log.e(TAG, "保存离线基准数据失败: " + e.getMessage());
        }
    }
    
    /**
     * 加载离线计算的基准数据
     */
    public OfflineBaseData loadOfflineBaseData() {
        try {
            if (!hasOfflineBaseData()) {
                return null;
            }
            
            OfflineBaseData data = new OfflineBaseData();
            data.petId = prefs.getString(KEY_PET_ID, "");
            data.petName = prefs.getString(KEY_PET_NAME, "我的宠物");
            data.prefabName = prefs.getString(KEY_PREFAB_NAME, "Pet_CatBrown");
            data.baseEnergy = prefs.getInt(KEY_BASE_ENERGY, 100);
            data.baseSatiety = prefs.getInt(KEY_BASE_SATIETY, 100);
            data.baseIsBored = prefs.getBoolean(KEY_BASE_IS_BORED, false);
            data.baseTimestamp = prefs.getLong(KEY_BASE_TIMESTAMP, System.currentTimeMillis());
            data.lastCalculationTime = prefs.getLong(KEY_LAST_CALCULATION_TIME, System.currentTimeMillis());
            
            return data;
        } catch (Exception e) {
            Log.e(TAG, "加载离线基准数据失败: " + e.getMessage());
            return null;
        }
    }
    
    /**
     * 更新最后离线计算时间
     */
    public void updateOfflineTimestamp(long timestamp) {
        try {
            long oldTimestamp = prefs.getLong(KEY_LAST_CALCULATION_TIME, 0);
            SharedPreferences.Editor editor = prefs.edit();
            editor.putLong(KEY_LAST_CALCULATION_TIME, timestamp);
            editor.apply();
            
            // Log.d(TAG, "离线计算时间已更新: " + oldTimestamp + " -> " + timestamp);
        } catch (Exception e) {
            Log.e(TAG, "更新离线计算时间失败: " + e.getMessage());
        }
    }
    
    /**
     * 获取最后离线计算时间
     */
    public long getLastOfflineCalculationTime() {
        return prefs.getLong(KEY_LAST_CALCULATION_TIME, 0);
    }
    
    /**
     * 检查是否有离线基准数据
     */
    public boolean hasOfflineBaseData() {
        return prefs.contains(KEY_BASE_TIMESTAMP) && 
               prefs.contains(KEY_BASE_ENERGY) && 
               prefs.contains(KEY_BASE_SATIETY);
    }
    
    /**
     * 清除所有离线数据
     */
    public void clearOfflineData() {
        try {
            SharedPreferences.Editor editor = prefs.edit();
            editor.clear();
            editor.apply();
            // Log.d(TAG, "离线数据已清除");
        } catch (Exception e) {
            Log.e(TAG, "清除离线数据失败: " + e.getMessage());
        }
    }
    
    /**
     * 离线基准数据模型
     */
    public static class OfflineBaseData {
        public String petId;
        public String petName;
        public String prefabName;
        public int baseEnergy;
        public int baseSatiety;
        public boolean baseIsBored;
        public long baseTimestamp;
        public long lastCalculationTime;
        
        @Override
        public String toString() {
            return "OfflineBaseData{" +
                    "petId='" + petId + '\'' +
                    ", petName='" + petName + '\'' +
                    ", baseEnergy=" + baseEnergy +
                    ", baseSatiety=" + baseSatiety +
                    ", baseIsBored=" + baseIsBored +
                    ", baseTimestamp=" + baseTimestamp +
                    ", lastCalculationTime=" + lastCalculationTime +
                    '}';
        }
    }
}
