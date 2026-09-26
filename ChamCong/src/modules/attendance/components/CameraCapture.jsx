import { useRef, useState, useEffect } from "react";

// Chụp ảnh qua webcam theo phong cách marixa:
// - Chờ: nút "Chụp ảnh" to viền chấm
// - Bật: preview video to + nút "Chụp"
// - Chụp xong: xem trước + "Chụp lại"
const CameraCapture = ({ onPhoto }) => {
    const videoRef = useRef(null);
    const streamRef = useRef(null);
    const [active, setActive] = useState(false);
    const [error, setError] = useState("");
    const [shot, setShot] = useState(null);

    const stop = () => {
        streamRef.current?.getTracks().forEach((t) => t.stop());
        streamRef.current = null;
        setActive(false);
    };

    // Dừng camera khi rời trang
    useEffect(
        () => () => {
            streamRef.current?.getTracks().forEach((t) => t.stop());
        },
        []
    );

    const start = async () => {
        try {
            setError("");
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { width: 640, height: 480, facingMode: "user" },
            });
            streamRef.current = stream;
            setActive(true);
        } catch {
            setError("Không truy cập được camera. Hãy cấp quyền và thử lại.");
        }
    };

    const capture = () => {
        const video = videoRef.current;
        if (!video || !video.videoWidth) {
            setError("Camera chưa sẵn sàng. Vui lòng thử lại.");
            return;
        }
        const canvas = document.createElement("canvas");
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext("2d").drawImage(video, 0, 0);
        canvas.toBlob(
            (blob) => {
                if (!blob) {
                    setError("Không chụp được ảnh.");
                    return;
                }
                setShot(
                    new File([blob], "attendance-photo.jpg", {
                        type: "image/jpeg",
                    })
                );
                onPhoto?.(blob);
            },
            "image/jpeg",
            0.85
        );
        stop();
    };

    const reset = () => {
        setShot(null);
        onPhoto?.(null);
        stop();
    };

    if (shot) {
        return (
            <div className="att-cam-done">
                <img src={URL.createObjectURL(shot)} alt="Ảnh đã chụp" />
                <div>
                    <strong>Đã chụp ảnh</strong>
                    <span className="att-muted">Sẽ gửi kèm khi vào / ra ca.</span>
                    <button type="button" onClick={reset}>
                        Chụp lại
                    </button>
                </div>
            </div>
        );
    }

    if (active) {
        return (
            <div className="att-cam">
                <video ref={videoRef} autoPlay playsInline muted />
                <button type="button" onClick={capture}>
                    Chụp ảnh
                </button>
            </div>
        );
    }

    return (
        <div>
            <div className="att-cam-idle">
                <button type="button" onClick={start}>
                    📷 Chụp ảnh
                </button>
            </div>
            {error && <p className="att-cam-error">{error}</p>}
        </div>
    );
};

export default CameraCapture;
