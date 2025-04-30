# UI 基礎介紹
![alt text](Resuorce\image.png)
基本上分為由左到右分為3個區塊，分別是Script Loader Region、Button Region、Protocol Setting Region。
## Script Loader Region
### Browse Json
可以選擇要使用的Script，選擇後會出現名稱，如下:

![alt text](Resuorce\image-1.png)

### Flow Status
表示連線狀態與腳本執行狀態，按下`Start Flow`會進行連線與執行Script，`End Flow`會停止執行並段開連線，如下:
- 紅燈表示尚未執行Script與連線。
- 橘燈表示連線完成Script Initial流程中。
- 綠燈表示連線完成Script Initial流程中等待Trigger觸發。

![alt text](Resuorce\image-2.png)

### LastTriggeredActionName 
顯示上一次被呼叫的TriggerName，如下:

![alt text](Resuorce\image-3.png)
### ExecutionResult 
顯示Button執行結果，如下:

![alt text](Resuorce\image-4.png)

## Button Region
在`Start Flow`讀取Script後會初始化這個Region將設定的按鈕名稱自動排列在這裡，點選按鈕後會執行Script設定的Action。

![alt text](Resuorce\image-5.png)
## Protocol Setting Region
在此區可以設定連線方式`Protocol`、`CPU Type`、`IPAddress`、`Port`等。

![alt text](Resuorce\image-6.png)
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

- 以上為各種時間設定相關，可以依據個人電腦環境彈性調整。例如，電腦效率好且連結PLC時，`Handshake_Timeout` 可以使用 4000ms；但如果是掛載 GXWork 模擬器，可以依照需求往上設定。建議主程式也要相應設定以確保流程順暢。

---

## 4. CarrierStorage Region
### 4.1 Name
用以區分屬於哪個`CarrierStage`，不應與其他的`CarrierStage`重複。

### 4.2 Material
#### 4.2.1 BindingMaterial
用以讓`CarrierStorage`與`GlobalVariable.Materials.CassetteFormat`進行綁定。

### 4.3 Initialize
#### 4.3.1 InitialActions
可以設定此`CarrierStorage`對應的初始化`Action`流程，此流程會在建立連線後執行。

### 4.4 Trigger
### TriggerActions
用以設定此`CarrierStorage`對應的被動觸發`Trigger`流程，此流程會根據條件決定是否執行。
| 主要欄位          | 說明 |
|:-------------|:-----|
| `Name` | 流程名稱，**為唯一值，如果與其他衝突時會造成設定遺失。** |
| `TriggerPoint` | 觸發條件集合設定，請參考 [4.4.1.1 TriggerPoint](#triggerpoint)。 |
| `Actions` | 被觸發時要執行的`Action`列表，請參考 [4.4.1.2 Actions](#actions)。 |

#### 4.4.1 TriggerPoint

- **Type**：定義多個 `Conditions` 之間的邏輯關係。
  - `"AND"`：所有 `Conditions` 成立時才觸發。
  - `"OR"`：其中一個 `Condition` 成立即觸發。

- **Conditions** ：定義觸發的具體條件。
  - `Action`：定義監控或檢查的動作類型。
    - `"Monitor"`：監控記憶位地址變化，當值與上次不同且符合 `ExceptedValue` 時觸發。
    - `"Specific"`：檢測記憶位值是否符合 `ExceptedValue`，每次檢查都判斷。
    - `"Change"`：偵測記憶位值的變化，當值與上次不同時觸發 (忽略 `ExceptedValue`)。
  - `ExceptedValue`：期望值，用於 `Monitor` 和 `Specific` 類型。
  - `Address`：監控或檢查的記憶位地址。

#### 4.4.2 Actions

`Actions` 是一個列表，定義了當 `TriggerPoint` 的條件滿足時，系統需要依序執行的操作流程。

##### 4.4.2.1 Action Structure

列表中的每個元素都是一個 `Action` 物件，其主要欄位如下：

| 欄位         | 說明 |
|:------------|:-----|
| `ActionName` | 定義要執行的操作類型，例如 `Write`, `Read`, `SecHandShake` 等。詳細說明請參考 [常見 ActionName 說明](#常見actionname說明)。 |
| `Inputs`     | 定義操作所需的輸入參數，通常包含操作目標的 `Address` 和提供數據來源的 `Value` 列表。其結構請參考 [4.4.2.2 Inputs Structure](#inputs-structure)。 |
| `Output`     | 定義操作的輸出結果，通常為空或用於接收讀取的值（目前實作中讀取結果主要透過 Inputs 處理讀取）。 |
| `DataTable` | 僅用於 `RobotMotion` Action，提供 Port 的 JobNo 和 WaferId 地址。詳細結構請參考 [常見 ActionName 說明 - RobotMotion](#常見actionname說明)。 |
| `ExecuteCondition` | 僅用於 `LoopAction` Action，設定循環執行子操作的條件。請參考 [4.4.2.3 ExecuteCondition](#executecondition)。 |
| `SubActions` | 僅用於 `LoopAction` Action，定義在循環中執行的子操作列表。請參考 [4.4.2.4 SubActions and PostActions](#subactions-and-postactions)。 |
| `PostActions` | 僅用於 `LoopAction` Action，定義在循環結束後執行的操作列表。請參考 [4.4.2.4 SubActions and PostActions](#subactions-and-postactions)。 |

##### 4.4.2.2 Inputs Structure

`Inputs` 欄位是一個物件，包含以下主要欄位：

| 欄位         | 說明 |
|:------------|:-----|
| `Address` | 操作目標的記憶位地址。格式通常為 "DeviceNameAddress" (例如 "W1000")。 |
| `Value`   | 一個 `InputValue` 物件列表，提供操作所需的具體數值或資料來源。列表中的每個元素都是一個 `InputValue` 物件，其結構與欄位意義請參考 [6. 特殊格式與欄位說明](#6-特殊格式與欄位說明)。 |

##### 4.4.2.3 ExecuteCondition

`ExecuteCondition` 欄位是 `LoopAction` 特有的可選欄位，用以設定循環執行子操作 (`SubActions`) 的條件。只有當此條件滿足時，`LoopAction` 中的 `SubActions` 才會被執行。

| 欄位         | 說明 |
|:------------|:-----|
| `Comment` | 注解說明，便於理解條件作用。 |
| `Type` | 條件類型，目前支援 `"Equals"` (等於)。 |
| `Format` | 值類型，目前支援 `"Int"` (整數)。 |
| `Content` | 期望值，用於與循環中的當前元素值進行比較。 |

##### 4.4.2.4 SubActions and PostActions

`LoopAction` 包含 `SubActions` 和 `PostActions` 兩個列表，它們都是 `Action` 物件的集合，結構與欄位意義請參考 [4.4.2.1 Action Structure](#action-structure)。

- `SubActions`：在 `LoopAction` 循環的每次迭代中，如果 `ExecuteCondition` 條件滿足，則會依序執行此列表中的所有 `Action`。
- `PostActions`：在 `LoopAction` 的循環結束後，會依序執行此列表中的所有 `Action`。

---

## 5. Buttons Region

`Buttons` 區塊定義了使用者介面上的按鈕及其對應的操作流程。這些按鈕通常用於手動觸發一系列預定義的動作。

### 5.1 Button Structure

`Buttons` 是一個列表，列表中的每個元素代表一個按鈕，其主要欄位如下：

| 欄位           | 說明                                     |
|:--------------|:-----------------------------------------|
| `ButtonContent` | 按鈕上顯示的文字內容。                     |
| `Actions`      | 當按鈕被點擊時，依序執行的 `Action` 列表。其結構與欄位意義請參考 [4.4.2 Actions](#actions)。 |

---

## 常見 ActionName 說明

| ActionName     | 說明 | 預期 Inputs.Value 結構 | 備註 |
|:--------------|:-----|:--------------------|:-----|
| `Write` | 將值寫入指定記憶位 (`Inputs.Address`)。 | 包含一個或多個 `InputValue` 物件的列表，提供要寫入的數據。 | 支援 `InputValue` 的 `Type` 為 `KeyIn`, `Action` (僅限 Read), `GlobalVariable`。 |
| `Read` | 讀取記憶位值 (`Inputs.Address`)。 | 通常為空列表或包含一個 `InputValue` 指定讀取來源（目前實作中讀取結果主要在 `Action` 類型的 `InputValue` 中使用）。 | 讀取結果通常用於後續 Action 的 Inputs。 |
| `SecHandShake` | 執行互信確認流程 (被動方)。 | 包含一個 `InputValue`，`Type` 必須為 `KeyIn`，`Format` 為 `String`，`Content` 為主動方的握手地址。 | `Inputs.Address` 為被動方的握手地址。 |
| `Hold` | 暫停指定時間。 | 空列表。 | 暫停時間由 `GlobalVariable.Hold_Time` 設定。 |
| `RobotMotion` | 模擬機械手動作並報告狀態變化。 | 空列表。 | 需要額外的 `DataTable` 欄位提供 Port 的 JobNo 和 WaferId 地址。詳細結構請參考下方說明。 |
| `PriHandShake` | 執行互信確認流程 (主動方)。 | 包含一個 `InputValue`，`Type` 必須為 `KeyIn`，`Format` 為 `String`，`Content` 為被動方的握手地址。 | `Inputs.Address` 為主動方的握手地址。 |
| `Index` | 將指定記憶位 (`Inputs.Address`) 的值加一。 | 空列表。 | 忽略 `Inputs.Value` 的內容。 |
| `LoopAction` | 根據條件循環執行子操作。 | 包含一個 `InputValue`，其值用於決定循環的元素列表。 | 包含 `ExecuteCondition`, `SubActions`, `PostActions` 欄位。詳細說明請參考 [4.4.2.3 ExecuteCondition](#executecondition) 和 [4.4.2.4 SubActions and PostActions](#subactions-and-postactions)。 |

**RobotMotion Action 的 DataTable 結構**

`RobotMotion` Action 需要一個額外的 `DataTable` 欄位，其結構如下：

```json
"DataTable": {
  "Port1": {
    "JobNo": "Port1 JobNo Address",
    "WaferId": "Port1 WaferId Address"
  },
  "Port2": {
    "JobNo": "Port2 JobNo Address",
    "WaferId": "Port2 WaferId Address"
  },
  "Port3": {
    "JobNo": "Port3 JobNo Address",
    "WaferId": "Port3 WaferId Address"
  },
  "Port4": {
    "JobNo": "Port4 JobNo Address",
    "WaferId": "Port4 WaferId Address"
  }
}
```

其中，`PortX` (X 為 Port 編號 1-4) 物件包含 `JobNo` 和 `WaferId` 兩個欄位，其值為對應數據在 PLC 中的記憶位地址。這些地址用於在模擬機械手動作時讀取或寫入 JobNo 和 WaferId 數據。

---

## 6. 特殊格式與欄位說明

以下是 `InputValue` 物件中常見的欄位及其意義：

| 欄位             | 說明 | 適用於 Type | 備註 |
|:----------------|:-----|:-----------|:-----|
| `Comment` | 注解說明，便於理解此輸入值的用途。 | 所有 Type | 可選欄位。 |
| `Type` | 定義此輸入值的來源類型。 | 所有 Type | 必須欄位。可選值包括：<br/>`"KeyIn"`: 固定值，直接使用 `Content` 的內容。<br/>`"Action"`: 來自執行另一個 Action 的結果 (目前僅支援 `ActionName: "Read"`)。<br/>`"GlobalVariable"`: 參照 `GlobalVariable` 區塊中的參數。 |
| `Format` | 定義此輸入值的資料格式。 | `KeyIn`, `GlobalVariable` | 必須欄位。常見格式包括 `"Int"` (整數), `"String"` (字串), `"IntList"` (整數列表)。 |
| `Content` | 根據 `Type` 和 `Format`，表示具體的數值、字串、或 `GlobalVariable` 的路徑。 | `KeyIn`, `GlobalVariable` | 必須欄位。對於 `KeyIn`，是實際值；對於 `GlobalVariable`，是點分隔的路徑 (例如 `"Materials.CassetteFormat.Cassette1.WaferList.0.WaferId"`)。 |
| `Lens` | 定義資料的長度，主要用於字串或列表類型。 | `KeyIn`, `Action` | 可選欄位。對於 `KeyIn`，表示重複 `Content` 的次數 (Format 為 Int 時) 或字串長度 (Format 為 String 時)；對於 `Action` (Read)，表示要讀取的字數 (Word)。 |
| `ActionName` | 當 `Type` 為 `"Action"` 時，定義要執行的子 Action 類型。 | `Action` | 必須欄位 (當 Type 為 Action 時)。目前僅支援 `"Read"`。 |
| `Address` | 當 `Type` 為 `"Action"` 時，定義子 Action 的操作目標記憶位地址。 | `Action` | 必須欄位 (當 Type 為 Action 時)。 |
| `ElementUnit` | 定義從 `Action` (Read) 或 `GlobalVariable` 讀取數據時，如何將數據分割成元素列表，主要用於 `LoopAction` 的輸入。 | `Action`, `GlobalVariable` | 可選欄位。可選值包括：<br/>`"Bit"`: 將讀取的 Word 數據按位元分割成 0/1 列表。<br/>`"Word"`: 將讀取的 Word 數據作為 Word 列表。<br/>`"Amount"`: 讀取一個 Word 值，並根據該值生成指定數量的元素 (例如，讀取值為 5，則生成包含 5 個元素的列表)。 |
| `Floating` | 當 `Type` 為 `"Action"` 且在 `LoopAction` 中使用時，指示是否根據當前循環索引偏移 `Address`。 | `Action` | 可選欄位 (布林值)。如果為 `true`，則實際讀取地址為 `Address` + `當前循環索引` * `Lens`。 |

---

# Appendix

### 常見 Trigger 組合範例

| Type | Condition範例 |
|:-----|:--------------|
| AND | 所有地址值都符合條件 |
| OR | 其中一個地址值符合即觸發 |

### 常見 Action 範例

| Action | 說明 |
|:------|:-----|
| `Write W54A0 = Read W1D50` | 讀取地址 `W1D50` 的值，並將其寫入地址 `W54A0`。 |
| `SecHandShake W54BF = W1D5F` | 在地址 `W54BF` (被動方) 與地址 `W1D5F` (主動方) 之間執行互信確認流程。 |
| `LoopAction (Read W1153, Lens: 3, ElementUnit: Bit) with ExecuteCondition (Equals, Int, 1)` | 讀取地址 `W1153` 的 3 個 Word，將其轉換為位元列表。對於列表中的每個位元，如果其值等於 1，則執行 `SubActions`。 |