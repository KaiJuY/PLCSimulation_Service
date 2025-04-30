# Script Config 

---

## 1. Introduction

本文件主要在說明Config的資料結構和各欄位意義，供使用者依規範自定義流程腳本，方便開發與系統解析。

---

## 2. Overall Structure
目前結構分為3個區塊，區塊說明如下表格所示:
| 區塊         | 說明 |
|:------------|:-----|
| `GlobalVariable` | 全域參數（資料列表、位置表、定時設定） |
| `CarrierStorage` | 定義各Port的初始化與觸發流程 |
| `Buttons` | 定義按鈕與動作集合 |

---

## 3. GlobalVariable Region

### 3.1 Materials

- `CassetteFormat`：載具基本設定，包括 CassetteId 、WaferList。

### 3.2 PositionTable

- 定義裝置層級，包括 Name，RobotPosition，StageNo，SlotNo，BaseAddr等資料，目前僅提供ESWIN RobotMotion使用，其餘可以忽略此區塊。

### 3.3 Timing Settings

| 區塊         | 說明 |
|:------------|:-----|
| `Action_Interval` | 標示操作間隔(ms) |
| `Monitor_Interval` | 監控頻率(ms) |
| `Hold_Time` | 暫停時間(ms) |
| `Handshake_Timeout` | 交握超時時間(ms) |

- 以上與各種時間設定相關，可以依據個人電腦環境彈性調整，
例如電腦效率好且連結PLC`Handshake_Timeout`可以使用4000ms但如果是掛GXWork模擬可以依照需求往上設定到，另外建議主程式也要相應設定會比較順利。

---

## 4. CarrierStorage Region
### 4.1 Name
用以區分屬於哪個`CarrierStage`，不應與其他的`CarrierStage`重複。

### 4.1 Material
#### 4.1.1 BindingMaterial
用以讓`CarrierStorage`與`GlobalVariable.Materials.CassetteFormat`進行綁定。

### 4.3 Initialize
#### 4.3.1 InitialActions
可以設定此`CarrierStorage`對應的初始化`Action`流程，此流程會在建立連線後執行。

### 4.4 Trigger
### TriggerActions
用以設定此`CarrierStorage`對應的被動觸發`Trigger`流程，此流程會根據條件決定是否執行。
| 主要欄位          | 說明 |
|:-------------|:-----|
| `Name` | 流程名稱 |
| `TriggerPoint` | 觸發條件集合設定 |
| `Actions` | 被觸發時要執行的`Action`列表 |
- 特別注意`Name`**為唯一值，如果與其他衝突時會造成設定遺失。**
#### 4.4.1 TriggerPoint

- **Type**：
  - `"AND"`：所有`Conditions`成立時才觸發
  - `"OR"`：其中一個`Condition`成立即觸發

- **Conditions** ：
  - `Action`：
    - `"Monitor"`：監控記憶位地址變化
    - `"Specific"`：檢測記憶位值是否符合
    - `"Change"`：偵測值的變化
  - `ExceptedValue`：期望值
  - `Address`：記憶位地址

#### 4.4.2 Actions

- 包括：
  - `ActionName`（如 `Write`/`Read`/`SecHandShake`/`Hold`/`RobotMotion`）
  - `Inputs`（操作目標 Address + Value）
  - `Output`（有需要時會返回，通常空發）

##### 常見ActionName說明

| ActionName     | 說明 |
|:--------------|:-----|
| `Write` | 將值寫入指定記憶位 |
| `Read` | 讀取記憶位值 |
| `SecHandShake` | 互信確認流程 |
| `Hold` | 暫停指定時間 |
| `RobotMotion` | 機械手自動動作 |

---

## 5. Buttons Region

- 作為人機介面按鈕，執行一系列連續操作（與Actions結構相同）。

---

## 6. 特殊格式與欄位說明

| 欄位             | 說明 |
|:----------------|:-----|
| `Comment` | 注解說明，便於理解記憶位作用 |
| `Type` | 值類型，`KeyIn` (固定值) ，`Action` (讀取)，`GlobalVariable` (參照全域參數) |
| `Format` | 資料格式，如 `Int`，`String` |
| `Content` | 真正要輸入或輸出的值 |
| `Lens` | 長度（透過String/Array點用） |

---

# Appendix

### 常見Trigger組合範例

| Type | Condition範例 |
|:-----|:--------------|
| AND | 所有地址值都符合條件 |
| OR | 其中一個地址值符合即觸發 |

### 常見Action範例

| Action | 說明 |
|:------|:-----|
| `Write W54A0 = Read W1D50` | 讀取值後寫入指定地址 |
| `SecHandShake W54BF = W1D5F` | 互信確認 |

---