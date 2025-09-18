package com.zher.meow.widget;

import org.json.JSONException;
import org.json.JSONObject;

/**
 * 小组件数据模型
 * 对应Unity端的AndroidWidgetData
 */
public class WidgetData {
    public boolean widgetEnabled;
    public String selectedPetId;
    public PetData selectedPetData;
    public String lastUpdateTime;
    
    public WidgetData() {
        this.widgetEnabled = true;
        this.selectedPetId = "";
        this.selectedPetData = null;
        this.lastUpdateTime = "";
    }
    
    /**
     * 从JSON字符串创建WidgetData对象
     */
    public static WidgetData fromJson(String jsonString) throws JSONException {
        JSONObject jsonObject = new JSONObject(jsonString);
        
        WidgetData widgetData = new WidgetData();
        widgetData.widgetEnabled = jsonObject.optBoolean("widgetEnabled", true);
        widgetData.selectedPetId = jsonObject.optString("selectedPetId", "");
        widgetData.lastUpdateTime = jsonObject.optString("lastUpdateTime", "");
        
        // 解析宠物数据
        JSONObject petDataJson = jsonObject.optJSONObject("selectedPetData");
        if (petDataJson != null) {
            widgetData.selectedPetData = PetData.fromJson(petDataJson);
        }
        
        return widgetData;
    }
    
    /**
     * 转换为JSON字符串
     */
    public String toJson() throws JSONException {
        JSONObject jsonObject = new JSONObject();
        jsonObject.put("widgetEnabled", widgetEnabled);
        jsonObject.put("selectedPetId", selectedPetId);
        jsonObject.put("lastUpdateTime", lastUpdateTime);
        
        if (selectedPetData != null) {
            jsonObject.put("selectedPetData", selectedPetData.toJson());
        }
        
        return jsonObject.toString();
    }
    
    @Override
    public String toString() {
        return "WidgetData{" +
                "widgetEnabled=" + widgetEnabled +
                ", selectedPetId='" + selectedPetId + '\'' +
                ", selectedPetData=" + selectedPetData +
                ", lastUpdateTime='" + lastUpdateTime + '\'' +
                '}';
    }
}
