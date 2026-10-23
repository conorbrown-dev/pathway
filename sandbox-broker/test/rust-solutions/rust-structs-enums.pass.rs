enum Enrollment {
    Invited { email: String },
    Active { email: String, completed: u32 },
    Cancelled { reason: String },
}

fn accept(enrollment: Enrollment) -> Enrollment {
    match enrollment {
        Enrollment::Invited { email } => Enrollment::Active { email, completed: 0 },
        other => other,
    }
}

fn complete_lesson(enrollment: Enrollment) -> Enrollment {
    match enrollment {
        Enrollment::Active { email, completed } => Enrollment::Active { email, completed: completed + 1 },
        other => other,
    }
}

fn summary(enrollment: &Enrollment) -> String {
    match enrollment {
        Enrollment::Invited { email } => format!("invited {email}"),
        Enrollment::Active { completed, .. } => format!("active {completed}"),
        Enrollment::Cancelled { reason } => format!("cancelled {reason}"),
    }
}
