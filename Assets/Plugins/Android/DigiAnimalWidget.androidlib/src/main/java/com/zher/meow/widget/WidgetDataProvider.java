package com.zher.meow.widget;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

/**
 * 小组件数据提供者
 * 统一管理游戏数据和离线数据，提供最佳数据源
 */
public class WidgetDataProvider {
    private static final String TAG = "WidgetDataProvider";
    private static final String GAME_DATA_PREFS = "widget_game_data";
    private static final String KEY_GAME_DATA_JSON = "game_data_json";
    
    private Context context;
    private OfflineDataManager offlineDataManager;
    private SharedPreferences gameDataPrefs;
    
    // 单例模式
    private static WidgetDataProvider instance;
    
    private WidgetDataProvider(Context context) {
        this.context = context.getApplicationContext();
        this.offlineDataManager = new OfflineDataManager(this.context);
        this.gameDataPrefs = this.context.getSharedPreferences(GAME_DATA_PREFS, Context.MODE_PRIVATE);
    }
    
    public static synchronized WidgetDataProvider getInstance(Context context) {
        if (instance == null) {
            instance = new WidgetDataProvider(context);
        }
        return instance;
    }
    
    /**
     * 获取当前最佳的宠物数据
     * 优先级：新鲜的游戏数据 > 离线计算数据 > 默认数据
     */
    public PetData getCurrentPetData() {
        try {
            // 获取游戏数据
            PetData gameData = getGameData();
            // Log.d(TAG, "游戏数据: " + (gameData != null ? gameData.petName + " (时间戳:" + gameData.lastUpdateTime + ")" : "null"));
            
            // 获取离线基准数据
            OfflineDataManager.OfflineBaseData offlineBaseData = offlineDataManager.loadOfflineBaseData();
            // Log.d(TAG, "离线基准数据: " + (offlineBaseData != null ? 
            //       "存在 (基准时间:" + offlineBaseData.baseTimestamp + ", 精力:" + offlineBaseData.baseEnergy + ", 饱食:" + offlineBaseData.baseSatiety + ")" : 
            //       "不存在"));
            
            // 判断使用哪个数据源
            boolean shouldUseGame = DataFreshnessChecker.shouldUseGameData(gameData, offlineBaseData);
            // Log.d(TAG, "数据源判断结果: " + (shouldUseGame ? "使用游戏数据" : "使用离线数据"));
            
            if (shouldUseGame) {
                // Log.d(TAG, "使用游戏数据");
                
                // 如果游戏数据更新鲜，更新离线基准
                if (gameData != null) {
                    updateOfflineBaseline(gameData);
                }
                
                return gameData;
            } else if (offlineBaseData != null && DataFreshnessChecker.isOfflineDataValid(offlineBaseData)) {
                // Log.d(TAG, "使用离线计算数据 - 立即执行计算");
                
                // 使用离线计算数据
                long currentTime = System.currentTimeMillis();
                long elapsedSeconds = (currentTime - offlineBaseData.baseTimestamp) / 1000;
                // Log.d(TAG, "离线计算: 基准时间=" + offlineBaseData.baseTimestamp + 
                //           ", 当前时间=" + currentTime + ", 经过=" + elapsedSeconds + "秒");
                
                PetData calculatedData = OfflineCalculator.calculateCurrentStats(offlineBaseData, currentTime);
                
                // if (calculatedData != null) {
                //     Log.d(TAG, "离线计算结果: 精力=" + calculatedData.energy + 
                //               ", 饱食=" + calculatedData.satiety + 
                //               ", 无聊=" + calculatedData.isBored);
                // }
                
                // 更新离线计算时间
                offlineDataManager.updateOfflineTimestamp(currentTime);
                
                return calculatedData;
            } else {
                // Log.d(TAG, "使用默认数据");
                return createDefaultPetData();
            }
            
        } catch (Exception e) {
            Log.e(TAG, "获取当前宠物数据失败: " + e.getMessage());
            return createDefaultPetData();
        }
    }
    
    /**
     * 处理游戏数据更新
     */
    public void handleGameDataUpdate(PetData gameData) {
        if (gameData == null) {
            Log.w(TAG, "游戏数据为空，忽略更新");
            return;
        }
        
        try {
            // 保存游戏数据
            saveGameData(gameData);
            
            // 更新离线基准数据
            updateOfflineBaseline(gameData);
            
            // Log.d(TAG, "游戏数据更新完成: " + gameData.petName);
            
        } catch (Exception e) {
            Log.e(TAG, "处理游戏数据更新失败: " + e.getMessage());
        }
    }
    
    /**
     * 刷新离线计算（手动刷新按钮调用）
     */
    public PetData refreshOfflineCalculation() {
        // Log.d(TAG, "手动刷新离线计算");
        
        try {
            // 立即执行离线计算
            OfflineDataManager.OfflineBaseData offlineBaseData = offlineDataManager.loadOfflineBaseData();
            
            if (offlineBaseData != null && DataFreshnessChecker.isOfflineDataValid(offlineBaseData)) {
                long currentTime = System.currentTimeMillis();
                PetData calculatedData = OfflineCalculator.calculateCurrentStats(offlineBaseData, currentTime);
                
                // 更新离线计算时间
                offlineDataManager.updateOfflineTimestamp(currentTime);
                
                // Log.d(TAG, "离线计算刷新完成");
                return calculatedData;
            } else {
                Log.w(TAG, "离线基准数据无效，返回默认数据");
                return createDefaultPetData();
            }
            
        } catch (Exception e) {
            Log.e(TAG, "刷新离线计算失败: " + e.getMessage());
            return createDefaultPetData();
        }
    }
    
    /**
     * 定期离线更新（每分钟调用）
     */
    public PetData periodicOfflineUpdate() {
        // Log.d(TAG, "定期离线更新");
        
        try {
            // 检查是否有游戏数据更新
            PetData gameData = getGameData();
            OfflineDataManager.OfflineBaseData offlineBaseData = offlineDataManager.loadOfflineBaseData();
            
            // 如果游戏数据更新鲜，使用游戏数据并更新基准
            if (DataFreshnessChecker.shouldUseGameData(gameData, offlineBaseData)) {
                if (gameData != null) {
                    updateOfflineBaseline(gameData);
                    return gameData;
                }
            }
            
            // 否则执行离线计算
            return refreshOfflineCalculation();
            
        } catch (Exception e) {
            Log.e(TAG, "定期离线更新失败: " + e.getMessage());
            return createDefaultPetData();
        }
    }
    
    /**
     * 更新离线基准数据
     */
    private void updateOfflineBaseline(PetData gameData) {
        if (gameData == null) {
            Log.w(TAG, "游戏数据为空，无法更新离线基准");
            return;
        }
        
        try {
            long currentTime = System.currentTimeMillis();
            // Log.d(TAG, "更新离线基准数据: 宠物=" + gameData.petName + 
            //           ", 精力=" + gameData.energy + 
            //           ", 饱食=" + gameData.satiety + 
            //           ", 无聊=" + gameData.isBored + 
            //           ", 基准时间=" + currentTime);
            
            offlineDataManager.saveOfflineBaseData(gameData, currentTime);
            // Log.d(TAG, "离线基准数据保存成功");
        } catch (Exception e) {
            Log.e(TAG, "更新离线基准数据失败: " + e.getMessage());
        }
    }
    
    /**
     * 保存游戏数据到SharedPreferences
     */
    private void saveGameData(PetData gameData) {
        try {
            if (gameData == null) {
                return;
            }
            
            // 简单的JSON序列化（这里可以用更完善的JSON库）
            String jsonData = petDataToJson(gameData);
            
            SharedPreferences.Editor editor = gameDataPrefs.edit();
            editor.putString(KEY_GAME_DATA_JSON, jsonData);
            editor.putLong("save_time", System.currentTimeMillis());
            editor.apply();
            
        } catch (Exception e) {
            Log.e(TAG, "保存游戏数据失败: " + e.getMessage());
        }
    }
    
    /**
     * 从SharedPreferences获取游戏数据
     */
    private PetData getGameData() {
        try {
            String jsonData = gameDataPrefs.getString(KEY_GAME_DATA_JSON, null);
            if (jsonData == null || jsonData.isEmpty()) {
                // Log.d(TAG, "SharedPreferences中没有游戏数据");
                return null;
            }
            
            PetData gameData = jsonToPetData(jsonData);
            
            // 验证数据有效性
            if (!DataFreshnessChecker.isDataValid(gameData)) {
                Log.w(TAG, "游戏数据无效");
                return null;
            }
            
            // Log.d(TAG, "获取到游戏数据: " + gameData.petName + ", 时间戳: " + gameData.lastUpdateTime);
            return gameData;
            
        } catch (Exception e) {
            Log.e(TAG, "获取游戏数据失败: " + e.getMessage());
            return null;
        }
    }
    
    /**
     * 创建默认宠物数据
     */
    private PetData createDefaultPetData() {
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
        
        return defaultData;
    }
    
    /**
     * 简单的PetData转JSON（可以替换为更好的JSON库）
     */
    private String petDataToJson(PetData data) {
        if (data == null) {
            return "{}";
        }
        
        StringBuilder json = new StringBuilder();
        json.append("{");
        json.append("\"petId\":\"").append(escapeJson(data.petId)).append("\",");
        json.append("\"petName\":\"").append(escapeJson(data.petName)).append("\",");
        json.append("\"prefabName\":\"").append(escapeJson(data.prefabName)).append("\",");
        json.append("\"energy\":").append(data.energy).append(",");
        json.append("\"satiety\":").append(data.satiety).append(",");
        json.append("\"isBored\":").append(data.isBored).append(",");
        json.append("\"purchaseDate\":\"").append(escapeJson(data.purchaseDate)).append("\",");
        json.append("\"ageInDays\":").append(data.ageInDays).append(",");
        json.append("\"introduction\":\"").append(escapeJson(data.introduction)).append("\",");
        json.append("\"lastUpdateTime\":\"").append(escapeJson(data.lastUpdateTime)).append("\"");
        json.append("}");
        
        return json.toString();
    }
    
    /**
     * 简单的JSON转PetData（可以替换为更好的JSON库）
     */
    private PetData jsonToPetData(String json) {
        if (json == null || json.isEmpty()) {
            return null;
        }
        
        PetData data = new PetData();
        
        // 简单的字符串解析（生产环境建议使用JSON库）
        try {
            data.petId = extractJsonString(json, "petId");
            data.petName = extractJsonString(json, "petName");
            data.prefabName = extractJsonString(json, "prefabName");
            data.energy = extractJsonInt(json, "energy");
            data.satiety = extractJsonInt(json, "satiety");
            data.isBored = extractJsonBoolean(json, "isBored");
            data.purchaseDate = extractJsonString(json, "purchaseDate");
            data.ageInDays = extractJsonInt(json, "ageInDays");
            data.introduction = extractJsonString(json, "introduction");
            data.lastUpdateTime = extractJsonString(json, "lastUpdateTime");
        } catch (Exception e) {
            Log.e(TAG, "解析JSON失败: " + e.getMessage());
            return null;
        }
        
        return data;
    }
    
    // 简单的JSON解析辅助方法
    private String extractJsonString(String json, String key) {
        String pattern = "\"" + key + "\":\"";
        int start = json.indexOf(pattern);
        if (start == -1) return "";
        start += pattern.length();
        int end = json.indexOf("\"", start);
        if (end == -1) return "";
        return json.substring(start, end);
    }
    
    private int extractJsonInt(String json, String key) {
        String pattern = "\"" + key + "\":";
        int start = json.indexOf(pattern);
        if (start == -1) return 0;
        start += pattern.length();
        int end = json.indexOf(",", start);
        if (end == -1) end = json.indexOf("}", start);
        if (end == -1) return 0;
        try {
            return Integer.parseInt(json.substring(start, end));
        } catch (NumberFormatException e) {
            return 0;
        }
    }
    
    private boolean extractJsonBoolean(String json, String key) {
        String pattern = "\"" + key + "\":";
        int start = json.indexOf(pattern);
        if (start == -1) return false;
        start += pattern.length();
        int end = json.indexOf(",", start);
        if (end == -1) end = json.indexOf("}", start);
        if (end == -1) return false;
        return json.substring(start, end).equals("true");
    }
    
    private String escapeJson(String str) {
        if (str == null) return "";
        return str.replace("\"", "\\\"").replace("\n", "\\n").replace("\r", "\\r");
    }
    
    /**
     * 清除所有数据（用于测试和重置）
     */
    public void clearAllData() {
        try {
            offlineDataManager.clearOfflineData();
            gameDataPrefs.edit().clear().apply();
            // Log.d(TAG, "所有数据已清除");
        } catch (Exception e) {
            Log.e(TAG, "清除数据失败: " + e.getMessage());
        }
    }
}
