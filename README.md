# 🎓 SciGPACalculator

**SciGPACalculator** is a desktop GPA/CGPA calculator built for **Faculty of Science students at Cairo University**.

The application calculates **Semester GPA and cumulative CGPA** according to the college grading rules, including the special handling of **failed and repeated courses**. It also provides **What-If** and **Target GPA** tools to help students understand how future grades can affect their academic performance.

---

## ✨ Features

* Calculate **Semester GPA** for each semester.
* Calculate cumulative **CGPA** across all semesters.
* Apply the college's **failed-course retake rule** automatically.
* Import multiple courses from a **CSV file**.
* Paste course data directly from **Excel**.
* **What-If analysis** to simulate future grades and their effect on CGPA.
* **Target GPA** calculation to determine the average required in remaining courses.
* Automatically save and load academic records using **JSON**.
* Validate course and grade input.
* Display CGPA with visual indicators based on its value.

---

## 🛠️ Tech Stack

* **C#**
* **.NET 10**
* **WPF**
* **MVVM**
* **xUnit**
* **JSON** for local data persistence
* **CSV** import

---

## 📊 Grading System

Grades are entered as percentages from **0–100%** and converted into GPA points using the college's continuous grading scale.

| Grade | Percentage | Points    |
| ----- | ---------- | --------- |
| A     | 85% – 100% | 3.5 – 5.0 |
| B     | 75% – 84%  | 2.5 – 3.4 |
| C     | 65% – 74%  | 1.5 – 2.4 |
| D     | 60% – 64%  | 1.0 – 1.4 |
| F     | Below 60%  | 0         |

For grades of **60% or higher**, the application uses:

```text
Points = 1.0 + (Percentage - 60) × 0.1
```

For grades below 60%:

```text
Points = 0
```

For example, **92% = 4.2 points**.

---

## 🔄 Failed Course Retake Rule

One of the main purposes of the application is handling courses that were previously failed and later repeated successfully.

If a student fails a course and later passes it:

* The previous failed attempt is not counted as a separate full course in the cumulative calculation.
* The successful attempt contributes its earned points.
* The course contributes **2 × its credit hours** to the CGPA denominator.
* This `2H` cap remains the same regardless of how many times the course was previously failed.
* The rule applies to the **cumulative CGPA**, not the individual semester GPA.

### Example

If a 3-credit course was failed and later passed:

```text
Earned Points = Successful Grade Points × 3
CGPA Hours    = 3 × 2 = 6
```

The application identifies repeated courses by **exact course-name matching**. Therefore, the same course should be entered using the exact same name across attempts.

---

## 🏗️ Project Structure

The project is separated into three main parts:

```text
SciGPACalculator/
│
├── SciGPACalculator.Core/
│   ├── Models/
│   ├── Services/
│   └── Persistence/
│
├── SciGPACalculator.Tests/
│
└── SciGPACalculator.WPF/
    ├── ViewModels/
    ├── Views/
    ├── Converters/
    └── MvvmBase/
```

### Core

Contains the application's business logic independently from the UI:

* Course and academic record models
* GPA/CGPA calculations
* Retake policy
* What-If calculations
* Target GPA calculations
* CSV importing
* JSON persistence

### WPF

Contains the desktop user interface and follows the **MVVM** pattern.

### Tests

Contains the **xUnit** test suite covering the main calculation and data-handling scenarios.

Keeping the calculation logic separate from the WPF interface makes the core functionality easier to test and maintain.

---

## 💻 Requirements

### Running from Source

* Windows
* .NET SDK 10 or later

### Running the Published Application

The published executable does **not require a separate .NET installation**.

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/ali-sweilem/SciGPACalculator.git
cd SciGPACalculator
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Project

```bash
dotnet build
```

### 4. Run the Application

```bash
dotnet run --project SciGPACalculator.WPF
```

---

## 📦 Publish as an Executable

To create a Release build:

```bash
dotnet publish SciGPACalculator.WPF -c Release -o ./publish
```

The generated executable will be located in:

```text
publish/SciGPACalculator.WPF.exe
```

---

## 📥 Data Input

The application supports three ways to enter academic data.

### Manual Entry

Courses can be added directly from the application by entering:

* Course name
* Credit hours
* Percentage

Failed courses should remain as separate attempts when they are later retaken.

### CSV Import

Multiple courses can be imported using a CSV file.

Example:

```csv
الفصل,اسم المقرر,الساعات المعتمدة,النسبة
الفصل الأول 2023,الجبر,4,45
الفصل الثاني 2023,الجبر,4,85
الفصل الأول 2023,فيزياء 1,3,90
```

The importer validates each row individually and skips invalid rows instead of rejecting the entire file.

### Excel Paste

Course rows can also be copied directly from Excel and pasted into the application.

Expected column order:

```text
Course Name → Credit Hours → Percentage
```

---

## 🔮 What-If Analysis

The **What-If** tool allows students to simulate future grades and see their potential effect on the cumulative CGPA.

For example, a student can enter a hypothetical course and grade to calculate:

```text
Current CGPA
      ↓
Hypothetical Course
      ↓
Expected Grade
      ↓
Projected CGPA
```

If the course corresponds to a previously failed course, the retake rule is applied automatically.

---

## 🎯 Target GPA

The **Target GPA** tool calculates the average points required in remaining courses to reach a desired cumulative CGPA.

The application can also identify cases where the target is:

* Mathematically achievable
* Mathematically impossible
* Already guaranteed under the given conditions

---

## 💾 Data Storage

Academic records are stored locally as a JSON file:

```text
%AppData%\GpaCalculator\academic-record.json
```

The application automatically:

* Loads the saved record when it starts.
* Saves the record when it closes.

Because the data is stored locally, a backup can be created simply by copying the JSON file.

---

## 🧪 Testing

The project uses **xUnit** for automated testing.

Run the test suite with:

```bash
dotnet test
```

The current test suite contains **33 tests** covering:

* Failed-course and retake scenarios
* Semester GPA
* Cumulative CGPA
* What-If calculations
* Target GPA calculations
* CSV importing
* JSON persistence

---

## ⚠️ Known Limitations

* Repeated courses are currently identified by **exact course-name matching**.
* CGPA color thresholds are configured for the application's **0–5 scale**.
* The application does not currently maintain a complete graduation-requirements system for individual departments, such as:

  * Required and elective course plans
  * Credit-hour requirements by academic year
  * Course prerequisites
  * Department-specific graduation rules

---

## 🔭 Future Plans

Possible future improvements include:

* Department-specific study plans.
* Complete graduation requirement tracking.
* Course prerequisite management.
* Better course identification instead of relying only on course names.
* Additional academic planning features.

---

## 👨‍💻 Author

**Ali Sweilem**

Computer Science Student
Faculty of Science — Cairo University

---

## 📄 License

This project is intended for educational and personal use.
