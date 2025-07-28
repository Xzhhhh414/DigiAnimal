#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#if __has_include(<WidgetKit/WidgetKit.h>)
#import <WidgetKit/WidgetKit.h>
#endif

// App Group标识符，用于与Widget共享数据
static NSString *appGroupIdentifier = @"group.com.zher.digiAnimal";

// C函数接口，供Unity调用
extern "C" {
    
    /// 设置App Group标识符
    void _IOSSetAppGroupIdentifier(const char* identifier) {
        if (identifier != NULL) {
            appGroupIdentifier = [NSString stringWithUTF8String:identifier];
            NSLog(@"[UnityIOSBridge] App Group标识符已设置: %@", appGroupIdentifier);
        }
    }
    
    /// 设置共享数据到UserDefaults
    void _IOSSetSharedData(const char* key, const char* value) {
        if (key == NULL || value == NULL) {
            NSLog(@"[UnityIOSBridge] 警告: key或value为空");
            return;
        }
        
        NSString *nsKey = [NSString stringWithUTF8String:key];
        NSString *nsValue = [NSString stringWithUTF8String:value];
        
        NSUserDefaults *sharedDefaults = nil;
        
        if (appGroupIdentifier.length > 0) {
            // 使用App Group共享数据
            sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
        } else {
            // 回退到标准UserDefaults
            sharedDefaults = [NSUserDefaults standardUserDefaults];
        }
        
        if (sharedDefaults != nil) {
            [sharedDefaults setObject:nsValue forKey:nsKey];
            [sharedDefaults synchronize];
            
            NSLog(@"[UnityIOSBridge] 数据已保存到SharedDefaults: %@ = %@", nsKey, nsValue);
        } else {
            NSLog(@"[UnityIOSBridge] 错误: 无法获取SharedDefaults");
        }
    }
    
    /// 设置共享图片数据（空实现，Widget使用静态资源）
    void _IOSSetSharedImage(const char* key, const char* imagePath) {
        // 注意：Widget使用Pet target中Assets.xcassets的静态图片资源
        // Unity只需传递prefabName（如"Pet_CatBrown"），Widget会自动拼接图片名称
        // 此函数保留为空实现，避免链接错误
        NSLog(@"[UnityIOSBridge] _IOSSetSharedImage被调用但忽略 - Widget使用静态图片资源");
    }

    /// 更新Live Activity（内部函数）
    void _IOSUpdateLiveActivity() {
        if (@available(iOS 16.1, *)) {
            // 通过反射调用主Target中的LiveActivityManager
            Class managerClass = NSClassFromString(@"PatPat.LiveActivityManager");
            if (managerClass) {
                id sharedManager = [managerClass performSelector:@selector(shared)];
                if (sharedManager) {
                    [sharedManager performSelector:@selector(updateLiveActivity)];
                    NSLog(@"[UnityIOSBridge] Live Activity更新请求已发送");
                }
            }
        } else {
            NSLog(@"[UnityIOSBridge] 警告: iOS版本低于16.1，不支持Live Activity");
        }
    }

    /// 更新Live Activity数据
    void _IOSUpdateWidgetData() {
        dispatch_async(dispatch_get_main_queue(), ^{
            _IOSUpdateLiveActivity();
        });
    }
    
    /// 启动Live Activity
    void _IOSStartLiveActivity() {
        if (@available(iOS 16.1, *)) {
            dispatch_async(dispatch_get_main_queue(), ^{
                // 通过反射调用主Target中的LiveActivityManager
                Class managerClass = NSClassFromString(@"PatPat.LiveActivityManager");
                if (managerClass) {
                    id sharedManager = [managerClass performSelector:@selector(shared)];
                    if (sharedManager) {
                        BOOL result = [(NSNumber*)[sharedManager performSelector:@selector(startLiveActivity)] boolValue];
                        if (result) {
                            NSLog(@"[UnityIOSBridge] Live Activity启动成功");
                        } else {
                            NSLog(@"[UnityIOSBridge] Live Activity启动失败");
                        }
                    }
                } else {
                    NSLog(@"[UnityIOSBridge] 错误: 无法找到LiveActivityManager类");
                }
            });
        } else {
            NSLog(@"[UnityIOSBridge] 警告: iOS版本低于16.1，不支持Live Activity");
        }
    }
    
    /// 停止Live Activity
    void _IOSStopLiveActivity() {
        if (@available(iOS 16.1, *)) {
            dispatch_async(dispatch_get_main_queue(), ^{
                // 通过反射调用主Target中的LiveActivityManager
                Class managerClass = NSClassFromString(@"PatPat.LiveActivityManager");
                if (managerClass) {
                    id sharedManager = [managerClass performSelector:@selector(shared)];
                    if (sharedManager) {
                        [sharedManager performSelector:@selector(stopLiveActivity)];
                        NSLog(@"[UnityIOSBridge] Live Activity停止请求已发送");
                    }
                }
            });
        } else {
            NSLog(@"[UnityIOSBridge] 警告: iOS版本低于16.1，不支持Live Activity");
        }
    }
    
    /// 检查Live Activity是否活跃
    bool _IOSIsLiveActivityActive() {
        if (@available(iOS 16.1, *)) {
            Class managerClass = NSClassFromString(@"PatPat.LiveActivityManager");
            if (managerClass) {
                id sharedManager = [managerClass performSelector:@selector(shared)];
                if (sharedManager) {
                    BOOL result = [(NSNumber*)[sharedManager performSelector:@selector(isLiveActivityActive)] boolValue];
                    return result;
                }
            }
        }
        return false;
    }
    
    /// 获取共享数据（用于调试）
    const char* _IOSGetSharedData(const char* key) {
        if (key == NULL) {
            return NULL;
        }
        
        NSString *nsKey = [NSString stringWithUTF8String:key];
        NSUserDefaults *sharedDefaults = nil;
        
        if (appGroupIdentifier.length > 0) {
            sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
        } else {
            sharedDefaults = [NSUserDefaults standardUserDefaults];
        }
        
        if (sharedDefaults != nil) {
            NSString *value = [sharedDefaults stringForKey:nsKey];
            if (value != nil) {
                // 返回C字符串（Unity负责释放内存）
                return strdup([value UTF8String]);
            }
        }
        
        return NULL;
    }
    
    /// 检查App Group是否可用
    bool _IOSIsAppGroupAvailable() {
        if (appGroupIdentifier.length == 0) {
            return false;
        }
        
        NSUserDefaults *sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
        return (sharedDefaults != nil);
    }
    
    /// 获取App Documents目录路径（用于图片存储）
    const char* _IOSGetDocumentsPath() {
        NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
        NSString *documentsDirectory = [paths objectAtIndex:0];
        return strdup([documentsDirectory UTF8String]);
    }
    

    
    /// 初始化iOS桥接系统
    void _IOSInitializeBridge() {
        NSLog(@"[UnityIOSBridge] iOS桥接系统已初始化");
        
        // 可以在这里执行一些初始化逻辑
        // 比如清理旧数据、设置默认值等
        
        // 验证App Group可用性
        if (appGroupIdentifier.length > 0) {
            if (_IOSIsAppGroupAvailable()) {
                NSLog(@"[UnityIOSBridge] App Group可用: %@", appGroupIdentifier);
            } else {
                NSLog(@"[UnityIOSBridge] 警告: App Group不可用: %@", appGroupIdentifier);
            }
        }
    }
    
    /// 清理共享数据
    void _IOSClearSharedData() {
        NSUserDefaults *sharedDefaults = nil;
        
        if (appGroupIdentifier.length > 0) {
            sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
        } else {
            sharedDefaults = [NSUserDefaults standardUserDefaults];
        }
        
        if (sharedDefaults != nil) {
            // 删除相关的键
            [sharedDefaults removeObjectForKey:@"WidgetData"];
            
            [sharedDefaults synchronize];
            NSLog(@"[UnityIOSBridge] 共享数据已清理");
        }
    }
    

    

    

    

}

// Objective-C类实现（如果需要更复杂的功能）
@interface UnityIOSBridgeHelper : NSObject
+ (void)logSharedData;
+ (NSDictionary*)getAllSharedData;
@end

@implementation UnityIOSBridgeHelper

+ (void)logSharedData {
    NSUserDefaults *sharedDefaults = nil;
    
    if (appGroupIdentifier.length > 0) {
        sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
    } else {
        sharedDefaults = [NSUserDefaults standardUserDefaults];
    }
    
    if (sharedDefaults != nil) {
        NSDictionary *dict = [sharedDefaults dictionaryRepresentation];
        NSLog(@"[UnityIOSBridge] 当前共享数据: %@", dict);
    }
}

+ (NSDictionary*)getAllSharedData {
    NSUserDefaults *sharedDefaults = nil;
    
    if (appGroupIdentifier.length > 0) {
        sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroupIdentifier];
    } else {
        sharedDefaults = [NSUserDefaults standardUserDefaults];
    }
    
    return [sharedDefaults dictionaryRepresentation];
}

@end 