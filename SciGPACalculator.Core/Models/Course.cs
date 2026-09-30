namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// يمثل مقرر واحد (سطر واحد في بيانات الطالب).
    /// ملاحظة: نفس المادة ممكن يكون ليها أكتر من سجل Course لو اتكررت (رسوب ثم نجاح)،
    /// وده اللي بيربط بينهم هو CourseCode مش أي حاجة تانية.
    /// </summary>
    public class Course
    {
        /// <summary>
        /// معرّف ثابت للمادة (مثلاً "MATH101" أو الاسم نفسه لو مفيش كود رسمي).
        /// ده المفتاح اللي هيربط كل محاولات نفس المادة ببعض وقت حساب الـ Retake.
        /// لازم يتكتب بنفس الصيغة بالظبط في كل مرة (هنحل ده لاحقًا بـ Dropdown في الواجهة
        /// بدل ما يتكتب يدويًا كل مرة، لتفادي أخطاء الكتابة).
        /// </summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>
        /// الاسم المعروض للمادة (ممكن يبقى نفس CourseCode لو مفيش نظام أكواد رسمي).
        /// </summary>
        public string CourseName { get; set; } = string.Empty;

        public decimal CreditHours { get; set; }

        /// <summary>
        /// الدرجة كنسبة مئوية (0-100). Nullable لأن المادة ممكن تكون لسه معندهاش درجة —
        /// الحالة دي بتستخدم في أدوات الـ What-If والـ Target GPA لما بنحط درجة افتراضية.
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// true لو المادة دي اتفشلت قبل السجل ده (على أي محاولة سابقة بنفس الـ CourseCode).
        /// هيتحدد تلقائيًا من الكود، مش هيتحط يدويًا من المستخدم (هنبنيها في الـ Service الجاي)،
        /// لكن الحقل نفسه لازم يكون موجود من دلوقتي في الـ Model.
        /// </summary>
        public bool IsRetake { get; set; }

        /// <summary>
        /// الفصل اللي المادة دي اتسجلت فيه. Nullable لأن مقرر "متبقي على التخرج"
        /// (زي المادة اللي رسبت فيها ولسه معدتهاش) ممكن يتحط في قايمة الـ Target GPA
        /// من غير ما يبقى مربوط بترم محدد لسه.
        /// </summary>
        public int? SemesterId { get; set; }
    }
}
