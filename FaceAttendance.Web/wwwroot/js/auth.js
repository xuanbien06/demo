document.addEventListener("DOMContentLoaded", () => {
    // 1. Xử lý Animation chuyển đổi form Đăng nhập / Đăng ký
    const signUpButton = document.getElementById('signUp');
    const signInButton = document.getElementById('signIn');
    const container = document.getElementById('container');

    signUpButton.addEventListener('click', () => {
        container.classList.add("right-panel-active");
    });

    signInButton.addEventListener('click', () => {
        container.classList.remove("right-panel-active");
    });

    // 2. Xử lý Ẩn/Hiện mật khẩu
    const togglePasswords = document.querySelectorAll('.toggle-password');
    togglePasswords.forEach(icon => {
        icon.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            if (input.type === "password") {
                input.type = "text";
                this.classList.remove('fa-eye');
                this.classList.add('fa-eye-slash');
            } else {
                input.type = "password";
                this.classList.remove('fa-eye-slash');
                this.classList.add('fa-eye');
            }
        });
    });

    // 3. Hàm hiển thị Loading trên nút
    const toggleLoading = (buttonId, isLoading) => {
        const btn = document.getElementById(buttonId);
        const text = btn.querySelector('.btn-text');
        const icon = btn.querySelector('.loading-icon');

        if (isLoading) {
            btn.disabled = true;
            text.classList.add('d-none');
            icon.classList.remove('d-none');
        } else {
            btn.disabled = false;
            text.classList.remove('d-none');
            icon.classList.add('d-none');
        }
    };

    // 4. Xử lý form Đăng Ký (Gọi API /Auth/RegisterApi)
    document.getElementById("registerForm").addEventListener("submit", async (e) => {
        e.preventDefault();

        const fullName = document.getElementById("regFullName").value;
        const email = document.getElementById("regEmail").value;
        const password = document.getElementById("regPassword").value;
        const confirmPassword = document.getElementById("regConfirmPassword").value;

        if (password !== confirmPassword) {
            Swal.fire('Lỗi!', 'Mật khẩu xác nhận không khớp.', 'error');
            return;
        }

        toggleLoading("btnRegister", true);

        try {
            const response = await fetch('/Auth/RegisterApi', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName, email, password, confirmPassword })
            });
            const data = await response.json();

            if (response.ok && data.success) {
                Swal.fire('Thành công!', data.message, 'success').then(() => {
                    // Tự động trượt về form Đăng Nhập
                    document.getElementById("registerForm").reset();
                    container.classList.remove("right-panel-active");
                });
            } else {
                Swal.fire('Lỗi!', data.message || "Có lỗi xảy ra", 'error');
            }
        } catch (error) {
            Swal.fire('Lỗi!', 'Không thể kết nối đến máy chủ.', 'error');
        } finally {
            toggleLoading("btnRegister", false);
        }
    });

    // 5. Xử lý form Đăng Nhập (Gọi API /Auth/LoginApi)
    document.getElementById("loginForm").addEventListener("submit", async (e) => {
        e.preventDefault();

        const email = document.getElementById("loginEmail").value;
        const password = document.getElementById("loginPassword").value;
        const rememberMe = document.getElementById("rememberMe").checked;

        toggleLoading("btnLogin", true);

        try {
            const response = await fetch('/Auth/LoginApi', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password, rememberMe })
            });
            const data = await response.json();

            if (response.ok && data.success) {
                Swal.fire({
                    title: 'Đăng nhập thành công!',
                    text: 'Đang chuyển hướng hệ thống...',
                    icon: 'success',
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    // Chuyển hướng về trang chủ
                    window.location.href = '/Home/Index';
                });
            } else {
                Swal.fire('Thất bại!', data.message || "Email hoặc mật khẩu sai", 'error');
            }
        } catch (error) {
            Swal.fire('Lỗi!', 'Không thể kết nối đến máy chủ.', 'error');
        } finally {
            toggleLoading("btnLogin", false);
        }
    });
});