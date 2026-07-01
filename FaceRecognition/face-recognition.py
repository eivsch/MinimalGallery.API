import os
import face_recognition

print('Running face recognition...')

unknown_people_folder = "./unknown_people"
known_people_folder = "./known_people"

for image_path in os.listdir(unknown_people_folder):
    print(f"Processing image: {image_path}")
    image = face_recognition.load_image_file(os.path.join(unknown_people_folder, image_path))
    unknown_faces = face_recognition.face_locations(image)

    print(f"Found {len(unknown_faces)} face(s) in {image_path}")

    # Recognize faces in the image
    for known_face in os.listdir(known_people_folder):
        known_image = face_recognition.load_image_file(os.path.join(known_people_folder, known_face))
        known_face_encoding = face_recognition.face_encodings(known_image)[0]

        # Compare the known face with the faces found in the unknown image
        for unknown_face in unknown_faces:
            unknown_face_encoding = face_recognition.face_encodings(image, [unknown_face])[0]
            results = face_recognition.compare_faces([known_face_encoding], unknown_face_encoding)

            if results[0]:
                print(f"Match found: {known_face} in {image_path}")
            else:
                print(f"No match for {known_face} in {image_path}")
